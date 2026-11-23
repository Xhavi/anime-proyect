# Reproductores externos (proveedores similares a AnimeAV1)

La API admite fuentes múltiples por episodio, sin almacenar archivos de video. Los nombres `MP4Upload`, `Mega`, `PixelDrain` y `TeraBox` pueden usarse como etiquetas de proveedor, **pero el proyecto no extrae, replica, descarga ni enlaza automáticamente episodios ajenos**.

## Seguridad y autorizaciones
- Solo introducir videos propios o con licencia que permita reproducción y monetización en nuestro dominio.
- El administrador debe declarar `distributionAuthorized: true` y configurar explícitamente en el servidor `NEBULA_VIDEO_HOSTS` con los hosts permitidos por el titular y el proveedor.
- No hay dominios habilitados de fábrica.
- Cada URL debe ser HTTPS y coincidir exactamente con un host de la lista; no se permiten subdominios implícitos.
- `embed` usa un iframe restringido; `direct` usa video HTML5.
- `X-Admin-Key` es un secreto de servidor: nunca colocarlo en frontend.
- El almacenamiento del MVP usa SQLite con creación de tabla idempotente para instalaciones antiguas; se requieren migraciones EF Core antes de producción.

## Configuración local (ejemplo ficticio)
```bash
NEBULA_VIDEO_HOSTS=videos.tu-dominio.com
NEBULA_ADMIN_KEY=una-clave-segura-de-32-caracteres-o-mas
```

## Crear una fuente autorizada
Enviar POST `/api/admin/episodes/{id}/sources` con encabezado `X-Admin-Key` y JSON:
```json
{
  "provider": "Mi CDN",
  "kind": "embed",
  "url": "https://videos.tu-dominio.com/embed/demo",
  "quality": "1080p",
  "distributionAuthorized": true
}
```
Luego consultar `GET /api/anime/{slug}/episodes/{number}/sources`. Las fuentes no se verán hasta que el administrador las registre.

**Importante:** los proveedores comerciales pueden prohibir embeds, hotlinking o distribución pública incluso cuando se pueda abrir un enlace. Confirma licencias y sus términos antes de integrar URLs.
