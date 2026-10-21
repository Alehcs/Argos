# Argos: hoja de ruta de mejoras

Documento vivo: lo vamos afinando. Última actualización: 21 oct 2026.

## Principios

- **Ligera mientras juegas:** la mínima RAM, CPU, GPU y procesos posibles.
- **No se pierde ninguna función:** todo lo que hace hoy sigue funcionando: búfer, grabación, capturas, Smart Save (voz),
  audio del escritorio de todas las apps, micrófono, pistas separadas, editor, subtítulos y compartir. No solo son
  buenas jugadas: también los momentos graciosos con amigos.
- **Sin perder calidad:** el vídeo y el audio de los clips mantienen su calidad. El modo Rendimiento baja ajustes por
  defecto, pero el usuario puede subirlos.
- **No se rompe nada:** cada cambio va en su propia rama y se prueba en Windows (más los tests) antes de fusionarlo en master.
- **Personalizable:** lo que sea un intercambio va como opción, no como recorte. Las funciones que no todos quieren se
  pueden apagar.

## Decisiones tomadas

- **Nombre:** Argos, en todo. Kairos ya no aparece en ningún sitio: interfaz, instalador, código, carpetas ni archivos
  (rama `rename/argos-complete`). Solo quedan dos menciones a propósito:
  - El crédito al proyecto original en los README y en THIRD-PARTY-NOTICES, que la licencia GPL pide conservar.
  - La dirección del motor de subtítulos, que hoy se descarga del repo original (ver Seguridad).
- **Créditos:** solo los obligatorios, y nunca en la interfaz.
  - Las licencias de terceros (OBS, FFmpeg, whisper.cpp, el modelo Whisper, ONNX Runtime, las fuentes…) lo exigen,
    cambie o no el modelo.
  - La línea "basado en Kairos": Argos parte de su código, y la licencia GPL pide conservar los avisos del autor original.
- **Arreglos del original:** se pueden seguir trayendo. Git reconoce los archivos renombrados, aunque habrá algún
  conflicto pequeño de vez en cuando.
- **Logo:** lo diseña Alejandro y lo entregará. Hasta entonces, solo dos imágenes muestran el nombre viejo: el nombre
  dibujado en la barra lateral (`wordmark-en.png`, `wordmark-ar.png`) y los logos y capturas del README (`docs/media`).
  Habrá que rehacer las capturas.
- **Lenguaje:** seguimos en C#/.NET, sin reescribir en Rust. El trabajo pesado lo hacen OBS (C), la GPU y FFmpeg (C);
  reescribir ahorraría unas decenas de MB a cambio de meses de trabajo y mucho riesgo.

## Paso 0: medir (antes de cualquier cambio)

- [ ] PC con Windows y GPU para probar.
- [ ] Medir antes y después de cada cambio: RAM del programa (página Estado), CPU, GPU y FPS del juego (PresentMon o CapFrameX).
- [ ] Compilación y tests automáticos en GitHub (Windows) en cada cambio. Así se puede comprobar que compila sin tener
  un PC con Windows a mano.

## Fase 1: ganancias rápidas (aprobada; sin cambiar funciones)

- [ ] **Precompilar el código (ReadyToRun):** menos CPU al arrancar con Windows y en el primer guardado de cada sesión.
- [ ] **Ajustar el recolector de memoria de .NET:** algo menos de RAM y un hilo menos.
- [ ] **En pausa o esperando un juego, detener también la captura de vídeo y los medidores de audio** (hoy solo se para
  el búfer). Mientras el búfer graba no cambia nada. El micrófono sigue activo si Smart Save lo necesita (por ejemplo,
  al grabar o probar la frase).
- [ ] **Smart Save:** que los modelos de voz no reserven memoria de sobra.

## Fase 2: modos Normal y Rendimiento (aprobada)

- [ ] **Dos modos con valores por defecto adecuados:**
  - Normal: como hoy (calidad alta).
  - Rendimiento: menos carga en la GPU (ajustes más rápidos del codificador, sin doble pase ni cuantización adaptativa)
    y búfer corto por defecto.
- [ ] **En ambos modos se puede cambiar todo:** fps (30, 60…), resolución (1440p, 1080p…), calidad, duración, etc.
- [ ] **Aviso:** si Argos pierde fotogramas, sugiere el modo Rendimiento. Se mide lo que pierde Argos; medir los FPS del
  propio juego costaría recursos.
- [ ] **PCs sin codificador de vídeo en la gráfica:** hoy codifica con la CPU a 1440p60. Bajar solo a 1080p30 con un
  ajuste rápido y avisar.
- [ ] Equivalentes para NVIDIA, AMD e Intel.

## Duración del clip (pedido)

- **Hoy:** el búfer va de 15 s a 20 min en pasos fijos (15 s, 30 s, 1, 1,5, 2, 3, 4, 5, 7, 10, 15 y 20 min). Hay además
  un "clip rápido" opcional con otra tecla (10, 15, 20, 30, 45 o 60 s).
- [ ] **Opciones rápidas:** 30 s, 1, 5, 10, 15, 30 y 60 min, más **Personalizado**.
- [ ] **Valores por defecto:** Normal 5 min. Rendimiento 1 min (recomendado) o 30 s. *Por decidir.*
- [ ] Mantener el clip rápido con duración elegible.
- **Ojo:** los búferes de 30 y 60 min necesitan el búfer en disco (fase 3). En memoria, 1 hora a la calidad por defecto
  podría ocupar unos 25 GB de RAM.

## Fase 3: búfer en disco (opcional; necesario para búferes largos)

- **Hoy:** los últimos minutos viven en la RAM. Más minutos o más calidad significa más RAM (5 min por defecto: hasta ~2,7 GB).
- **En disco:** lo mismo, pero en el SSD. Guarda trozos de unos 10 s, borra los viejos y, al pulsar guardar, une los
  últimos sin volver a codificar.
- **Calidad:** idéntica a la de hoy, en vídeo y en audio (las pistas separadas incluidas). Los trozos son el mismo
  vídeo ya codificado; solo cambia dónde se guardan.
- **Espacio en el disco:** fijo. Ocupa lo que dura el búfer y nunca crece, porque los trozos viejos se borran solos.
  Por ejemplo: 5 min, hasta ~2 GB; 1 hora, hasta ~25 GB a la calidad por defecto (mucho menos en Rendimiento). Al cerrar
  Argos se borra todo, y al arrancar se limpian los restos de un cierre inesperado.
- **Desgaste:** se escribe sin parar, unos 10–27 GB por hora a la calidad por defecto. Un SSD moderno lo aguanta años.
  Solo para SSD, no para discos duros antiguos.
- **Ventaja:** el búfer casi no usa RAM, y permite búferes de 30 o 60 min.
- [ ] **Propuesta:** memoria para búferes cortos y disco automático para los largos (con aviso). El usuario puede elegir.

## Funciones opcionales (se pueden apagar del todo)

- [ ] **Un apartado "Funciones" en Ajustes** para activar o desactivar:
  - **Smart Save (voz):** guardar diciendo una frase. Ya viene apagado por defecto.
  - **Subtítulos automáticos:** Apagado, **Rápido** (modelo ligero, ~60–190 MB, varias veces más rápido, acierta algo
    menos) o **Preciso** (el actual, 574 MB).
- [ ] **Apagado de verdad:** no se descarga nada, no se carga nada y no aparece en el editor ni en la interfaz.

## Funciones nuevas (sin carga extra mientras juegas)

- [ ] **Marcadores (aprobado):** una tecla para marcar momentos durante grabaciones largas; en el editor aparecen en la
  línea de tiempo para saltar a ellos.
- [ ] **Perfiles por juego:** calidad, duración y tipo de captura según el juego.
- [ ] Logo de Argos (lo diseña Alejandro).

## Seguridad y confianza

- [ ] **Instalador de confianza:** GitHub compila el instalador solo, desde el código. Se firma, así que Windows deja de
  avisar de "editor desconocido". Y se publica su huella (hash SHA-256) para comprobar que nadie lo alteró. La firma es
  gratis con SignPath si el repo es público; si no, cuesta unos 10 USD al mes (aproximado).
- [ ] **Comprobar el enlace de "Abrir publicación"** (que sea de x.com) antes de abrirlo.
- [ ] **Quitar los interruptores de desarrollador en las versiones públicas:** `update-source.txt`, `ARGOS_DATA` y
  `ARGOS_FFMPEG`. Permiten cambiar de dónde vienen las actualizaciones o cargar componentes desde otra carpeta.
- [ ] **Motor de subtítulos propio:** los subtítulos necesitan unos archivos extra (~15 MB) que Argos descarga la primera
  vez. Hoy los descarga del GitHub de BasilForge. Que alguien los altere no es un riesgo, porque Argos rechaza cualquier
  archivo cuya huella no coincida. El riesgo es que BasilForge los borre: los subtítulos dejarían de funcionar.
  - **Decisión:** mantener el mismo motor y el mismo modelo (whisper.cpp 1.9.5 con Vulkan), pero compilarlo nosotros.
    GitHub lo compila solo desde el código oficial (`tools/build-whisper.ps1`) y lo publica en las releases de Argos.
    Después se quita la descarga desde BasilForge.
  - **Requisito:** que el repo de Argos sea público, o que haya un repo público solo para descargas.
  - **Descartado:** el proyecto oficial no publica una versión con Vulkan para Windows; solo CPU (lenta) o CUDA (solo
    NVIDIA, 270–650 MB). Cambiar de motor sería mucho trabajo y no quitaría créditos.
  - El modelo (574 MB) ya viene del sitio oficial de whisper.cpp en HuggingFace, no de BasilForge.
- [ ] **Dependabot (aprobado):** aviso automático cuando las librerías tienen actualizaciones o parches de seguridad.

## Pendientes

- [ ] Compilar y probar en Windows. Nada se ha compilado todavía (ni el primer renombrado ni el completo).
- [ ] Las actualizaciones automáticas no funcionarán mientras el repo sea privado.

## Registro

- **21 oct 2026:** copia independiente de BasilForge/Kairos, auditoría de seguridad y renombrado a Argos de todo lo
  visible (commit `e1c1a82`). Después, renombrado completo de código, carpetas y archivos (rama
  `rename/argos-complete`, fusionada en master) y esta hoja de ruta.
