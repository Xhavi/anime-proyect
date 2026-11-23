# Nébula Anime

Plataforma original de descubrimiento de anime, con interfaz Angular y API ASP.NET Core. Se mantiene también la demostración estática en `index.html` mientras se completa la migración.

## Estructura

- `frontend/` — Angular 22, búsqueda, catálogo, favoritos y páginas de episodios.
- `backend/Nebula.Api/` — ASP.NET Core 10, EF Core y SQLite.
- `docs/ARCHITECTURE.md` — instrucciones de desarrollo, endpoints y seguridad.
- `index.html` — demo anterior de GitHub Pages (no es el frontend Angular).

## Empezar

```bash
cd backend/Nebula.Api && dotnet run
# Otra terminal:
cd frontend && npm install && npm start
```

Abre http://localhost:4200.

Por defecto hay solo datos ficticios; no se sirven videos comerciales ni anuncios reales. No se garantiza ingreso publicitario. Para publicar contenido se necesitan derechos de distribución y cumplimiento de las políticas de publicidad.
