# Probar Argos en tu PC

Guía para instalar una versión de prueba, comprobar que todo funciona y medir cuánto consume mientras juegas.

## 1. Descargar

1. En GitHub, abre **Actions → Test build** y entra en la última ejecución en verde.
2. Abajo, en **Artifacts**, descarga `Argos-Setup-<versión>` (hay que haber iniciado sesión en GitHub).
3. Descomprime el zip: dentro está `Argos-Setup.exe`.

## 2. Instalar

1. Abre `Argos-Setup.exe`. Windows avisará de un "editor desconocido", porque el instalador todavía no está firmado:
   pulsa **Más información → Ejecutar de todas formas**.
2. Elige idioma y carpeta. No pide permisos de administrador.
3. Al terminar, Argos queda junto al reloj. Su símbolo **a color** quiere decir que ya está guardando los últimos minutos.

Para quitarlo: Configuración de Windows → Aplicaciones → Argos → Desinstalar. Tus clips no se borran.

## 3. Comprobar que todo funciona

- [ ] **Guardar clip** en plena partida (`Alt+F10`): aparece en la Biblioteca.
- [ ] El clip tiene **el sonido del juego, el de tus amigos (Discord u otra app) y tu micrófono**.
- [ ] **Captura de pantalla** (`Alt+F12`) y **grabación completa** (`Alt+F9` para empezar y parar).
- [ ] **Guardado por voz** (opcional): en Smart Save graba tu frase y guarda un clip diciéndola.
- [ ] **Editor**: recortar, añadir texto y subtítulos automáticos.
- [ ] **Compartir** (opcional): publicar en X, TikTok o Instagram, o copiar el clip.
- [ ] **Modos**: en Calidad, cambiar entre Normal y Rendimiento.

## 4. Medir cuánto consume

Haz esto una vez en **Normal** y otra en **Rendimiento** (página Calidad), en el mismo juego y el mismo sitio del mapa:

1. Juega al menos **5 minutos** en Normal (1 minuto en Rendimiento). Mientras el búfer se llena, la memoria de Argos
   sube; después se queda estable.
2. Abre el **Administrador de tareas** (`Ctrl+Shift+Esc`) → pestaña **Procesos** → busca **Argos** y anota:
   - **CPU** (%)
   - **Memoria** (MB). La página Estado de Argos también la muestra.
   - **GPU** (%). Si no ves la columna, haz clic derecho en los títulos de las columnas y marca **GPU**.
3. Anota los **FPS del juego** con Argos abierto y con Argos cerrado (clic derecho en su icono → Salir). Usa el contador
   del propio juego, el de Steam o el de NVIDIA/AMD.

Mándame:

- Tu procesador, tu tarjeta gráfica y tu memoria RAM.
- El juego, con su resolución y sus gráficos.
- Para cada modo: CPU, memoria y GPU de Argos, y los FPS con y sin Argos.

## 5. Si algo falla

Los registros de Argos están en `%APPDATA%\Argos\logs` (pega esa ruta en el Explorador). Mándame el archivo más
reciente y cuéntame qué hacías cuando falló.
