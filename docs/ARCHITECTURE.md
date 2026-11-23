# Arquitectura — Nébula Anime

## Estado
- `index.html` en raíz: demostración estática anterior. Se mantiene para GitHub Pages hasta aprobar migración.
- `frontend/`: aplicación Angular 22 standalone con enrutamiento, catálogo HTTP, favoritos locales, reproductor HTML5 de videos autorizados.
- `backend/Nebula.Api/`: ASP.NET Core 10 + EF Core/SQLite, API pública y administración mediante clave de entorno de servidor.
- Metadatos y capítulos de demostración inventados. No hay videos comerciales ni anuncios de terceros.

## API pública
`GET /api/health` · `GET /api/anime?q=&genre=&page=&pageSize=` · `GET /api/anime/{slug}` · `GET /api/anime/{slug}/episodes` · `GET /api/ads`

## API de administración
Todas requieren encabezado `X-Admin-Key`, comparado por hash en tiempo constante, y `NEBULA_ADMIN_KEY` de al menos 32 caracteres en el servidor:

- `GET /api/admin/anime`
- `POST /api/admin/anime`, `PUT /api/admin/anime/{id}`, `DELETE /api/admin/anime/{id}`
- `POST /api/admin/anime/{id}/episodes`
- `PUT /api/admin/episodes/{id}`, `DELETE /api/admin/episodes/{id}`
- `PUT /api/admin/ads/{code}`

No hay pantalla de login en Angular: introducir una clave secreta en una SPA pública sería inseguro. Este es un API administrativo para clientes confiables; antes de producción implementar autenticación con roles, HTTPS y controles antiabuso.

## Desarrollo
Requisitos: Node.js compatible con Angular 22, .NET SDK 10.

Terminal 1:
```bash
cd backend/Nebula.Api
dotnet restore
dotnet run
```
Terminal 2:
```bash
cd frontend
npm install
npm start
```
Abrir http://localhost:4200. La API local corre en http://localhost:5050.

Opcional para operaciones administrativas (solo terminal local):
```bash
export NEBULA_ADMIN_KEY="una-clave-aleatoria-muy-larga-de-minimo-32-caracteres"
export NEBULA_VIDEO_HOSTS="cdn.tu-dominio-autorizado.com"
```

**No configures anuncios ni videos no autorizados en producción.** `NEBULA_VIDEO_HOSTS` limita los hosts de videos configurables por el administrador. Este filtro técnico no reemplaza los permisos legales.

## Límites antes de producción
1. Ejecutar CI, instalar dependencias y verificar build Angular + .NET.
2. Pruebas de integración con SQLite temporal y respuestas de error.
3. Migraciones EF Core, seguridad real de administración, HTTPS, observabilidad y backups.
4. Renderizado SEO (SSR/prerender) y sitemap dinámico.
5. Catálogo con datos/autorizaciones reales y revisión de derechos.
6. Gestor de consentimiento y red de anuncios aprobada.
7. Despliegue separado de frontend y backend; Pages no aloja ASP.NET Core.
