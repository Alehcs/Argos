using Argos.Engine.Share;

namespace Argos.Tests;

/// <summary>Which saved cookies mean "signed in" to X, TikTok or Instagram, and which belong to each site.</summary>
public static class SiteAccountsTests
{
    [Test]
    public static void A_site_counts_as_signed_in_only_with_its_own_session_cookie()
    {
        var cookies = new[]
        {
            new SiteCookie("auth_token", ".x.com", "abc"),
            new SiteCookie("guest_id", ".x.com", "v1"),
            new SiteCookie("sessionid", ".instagram.com", "123"),
            new SiteCookie("sessionid", ".example.com", "zzz"),
        };
        Assert.True(SiteAccounts.IsSignedIn(SharePlatform.X, cookies), "X");
        Assert.True(SiteAccounts.IsSignedIn(SharePlatform.Instagram, cookies), "Instagram");
        Assert.True(!SiteAccounts.IsSignedIn(SharePlatform.TikTok, cookies), "TikTok: another site's sessionid does not count");
        Assert.True(SiteAccounts.IsSignedIn(SharePlatform.TikTok, [new SiteCookie("sessionid", ".tiktok.com", "s")]), "TikTok");
        Assert.True(!SiteAccounts.IsSignedIn(SharePlatform.X, [new SiteCookie("auth_token", ".x.com", "")]), "an emptied cookie is signed out");
    }

    [Test]
    public static void Cookies_of_a_site_and_its_subdomains_are_its_own_and_lookalikes_are_not()
    {
        Assert.True(SiteAccounts.Owns(SharePlatform.X, ".x.com") && SiteAccounts.Owns(SharePlatform.X, "api.x.com"), "x.com");
        Assert.True(SiteAccounts.Owns(SharePlatform.X, ".twitter.com"), "the old twitter.com");
        Assert.True(!SiteAccounts.Owns(SharePlatform.X, "max.com") && !SiteAccounts.Owns(SharePlatform.X, "x.com.evil.net"), "lookalikes");
        Assert.True(SiteAccounts.Owns(SharePlatform.TikTok, "www.tiktok.com") && !SiteAccounts.Owns(SharePlatform.TikTok, ".instagram.com"), "TikTok");
    }

    [Test]
    public static void The_last_sign_ins_are_kept_one_line_per_site_and_read_back()
    {
        var state = new Dictionary<SharePlatform, bool> { [SharePlatform.X] = true, [SharePlatform.TikTok] = false, [SharePlatform.Instagram] = true };
        string text = SiteAccounts.FormatKnown(state);
        Assert.Equal("X=1\nTikTok=0\nInstagram=1", text.Replace("\r", ""), "the file");
        var back = SiteAccounts.ParseKnown(text);
        Assert.True(back is { Count: 3 } && back[SharePlatform.X] && !back[SharePlatform.TikTok] && back[SharePlatform.Instagram], "read back");
        Assert.True(SiteAccounts.ParseKnown("") == null && SiteAccounts.ParseKnown("garbage\nCopy=1") == null, "nothing usable: not known");
        var partial = SiteAccounts.ParseKnown("X=1");
        Assert.True(partial is { Count: 3 } && partial[SharePlatform.X] && !partial[SharePlatform.TikTok], "a site not listed: not signed in");
    }

    /// <summary>"Open post" opens only the site's own https page, whatever the page put in its link.</summary>
    [Test]
    public static void Only_the_sites_own_https_page_opens_as_the_new_post()
    {
        Assert.Equal("https://x.com/argos/status/1", SiteAccounts.PostAddress(SharePlatform.X, "https://x.com/argos/status/1"), "a post on x.com");
        Assert.Equal("https://mobile.twitter.com/a/status/2", SiteAccounts.PostAddress(SharePlatform.X, "https://mobile.twitter.com/a/status/2"),
            "a subdomain of the old twitter.com");
        foreach (string? bad in new[]
                 {
                     "http://x.com/a/status/1", "file:///C:/Windows/System32/calc.exe", @"C:\Windows\System32\calc.exe", "ms-settings:privacy",
                     "javascript:alert(1)", "https://x.com.evil.net/status/1", "https://evil.net/x.com/status/1", "https://x.com@evil.net/status/1",
                     "status/1", "", null,
                 })
            Assert.Equal(null, SiteAccounts.PostAddress(SharePlatform.X, bad), bad ?? "no address");
        Assert.Equal(null, SiteAccounts.PostAddress(SharePlatform.TikTok, "https://x.com/a/status/1"), "another site's page");
    }

    [Test]
    public static void Each_site_has_its_sign_in_page()
    {
        foreach (var p in SiteAccounts.Sites)
        {
            var url = new Uri(SiteAccounts.SignInUrl(p));
            Assert.True(url.Scheme == "https" && SiteAccounts.Owns(p, url.Host), $"{p}: {url}");
        }
        Assert.Equal("X TikTok Instagram", string.Join(" ", SiteAccounts.Sites), "the sites, in the panel's order");
    }
}
