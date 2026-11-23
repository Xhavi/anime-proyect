import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink, RouterOutlet, Routes } from '@angular/router';
import { CatalogService } from './catalog.service';
import { Anime, Episode, PagedResult, VideoSource } from './models';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
@Component({selector:'app-root',standalone:true,imports:[RouterOutlet,RouterLink],template:`
<header class="topbar"><nav class="shell nav"><a class="logo" routerLink="/">NÉBULA<span>✦</span></a><div class="nav-links"><a routerLink="/">Inicio</a><a routerLink="/catalogo">Catálogo</a><a routerLink="/favoritos">♡ Favoritos</a><a routerLink="/admin">Administración</a></div></nav></header>
<main><router-outlet/></main>
<footer class="shell footer">NÉBULA ✦ · Plataforma de demostración · Únicamente contenido propio o autorizado · Publicidad desactivada por defecto</footer>
`})
export class AppComponent {}
@Component({selector:'app-catalog',standalone:true,imports:[CommonModule,FormsModule,RouterLink],template:`
<section class="hero"><div class="shell hero-content"><span class="eyebrow">✦ Descubre nuevas historias</span><h1>Tu próximo universo<br>empieza aquí.</h1><p>Explora series y episodios. Contenido de prueba hasta agregar títulos autorizados.</p><a class="btn" routerLink="/catalogo">Explorar catálogo →</a></div></section>
<section class="shell content"><div class="header"><div><h2>Explorar catálogo</h2><p>Encuentra historias por título o género.</p></div></div>
<div class="controls"><input aria-label="Buscar anime" [(ngModel)]="query" (ngModelChange)="load()" placeholder="Buscar series..."><select aria-label="Filtrar género" [(ngModel)]="genre" (ngModelChange)="load()"><option value="">Todos los géneros</option><option>Acción</option><option>Fantasía</option><option>Romance</option><option>Misterio</option></select></div>
@if(loading()){<p role="status">Cargando catálogo…</p>}
@if(error()){<p class="error" role="alert">{{error()}}</p>}
<div class="cards">@for(anime of result().items;track anime.id){<a class="anime-card" [routerLink]="['/anime',anime.slug]"><div class="poster">{{anime.genre==='Fantasía'?'🌌':anime.genre==='Acción'?'⚔️':'✦'}}</div><div class="card-info"><strong>{{anime.title}}</strong><span>{{anime.genre}} · {{anime.year}} · {{anime.episodeCount}} episodios</span></div></a>} @empty { @if(!loading()){<p>No hay series para estos filtros.</p>} }</div>
<div class="pager"><button [disabled]="page===1" (click)="changePage(-1)">Anterior</button><span>Página {{page}} · {{result().total}} títulos</span><button [disabled]="page*20>=result().total" (click)="changePage(1)">Siguiente</button></div>
<div class="ad-slot" role="note">ESPACIO PUBLICITARIO · No se cargan anuncios reales</div></section>
`})
export class CatalogComponent implements OnInit {
  private api=inject(CatalogService); query='';genre='';page=1;
  loading=signal(false);error=signal('');
  result=signal<PagedResult<Anime>>({items:[],total:0,page:1,pageSize:20});
  ngOnInit(){this.load();}
  load(){this.page=1;this.fetch();}
  changePage(delta:number){this.page+=delta;this.fetch();}
  private fetch(){this.loading.set(true);this.error.set('');this.api.search(this.query,this.genre,this.page).subscribe({next:r=>{this.result.set(r);this.loading.set(false)},error:()=>{this.error.set('No se pudo conectar con la API. Comprueba que el servidor esté activo.');this.loading.set(false)}});}
}
@Component({selector:'app-detail',standalone:true,imports:[CommonModule,RouterLink],template:`
<section class="shell content">@if(error()){<p class="error">{{error()}}</p>}
@if(anime();as item){<div class="detail"><div class="poster large">✦</div><div><span class="eyebrow">{{item.genre}} · {{item.year}}</span><h1>{{item.title}}</h1><p>{{item.description}}</p><button class="btn secondary" (click)="favorite(item.id)">{{api.favorites().includes(item.id)?'♥ Quitar de favoritos':'♡ Añadir a favoritos'}}</button><h2>Episodios</h2><div class="episodes">@for(ep of episodes();track ep.id){<a [routerLink]="['/ver',item.slug,ep.number]">Episodio {{ep.number}} · {{ep.title}}</a>} @empty {<p>No hay episodios publicados.</p>}</div></div></div>}</section>
`})
export class DetailComponent implements OnInit {
  api=inject(CatalogService);private route=inject(ActivatedRoute);anime=signal<Anime|null>(null);episodes=signal<Episode[]>([]);error=signal('');
  ngOnInit(){const slug=this.route.snapshot.paramMap.get('slug')||'';this.api.anime(slug).subscribe({next:a=>this.anime.set(a),error:()=>this.error.set('No se encontró la serie.')});this.api.episodes(slug).subscribe({next:v=>this.episodes.set(v),error:()=>this.episodes.set([])});}
  favorite(id:number){this.api.toggleFavorite(id);}
}
@Component({selector:'app-watch',standalone:true,imports:[CommonModule,RouterLink],template:`
<section class="shell content">
<a [routerLink]="['/anime',slug]">← Volver a la serie</a><h1>Episodio {{number}}</h1>
@if(sources().length){<div class="source-picker" aria-label="Servidores disponibles">
  @for(source of sources();track source.id){
    <button type="button" class="source-choice" [attr.aria-pressed]="selected()?.id===source.id"
      (click)="selectSource(source)">{{source.provider}} · {{source.quality}}</button>
  }
</div>}
@if(selected()?.kind==='embed' && safeEmbed()){
  <iframe class="video" [src]="safeEmbed()" title="Reproductor externo autorizado" allow="fullscreen; picture-in-picture"
   allowfullscreen referrerpolicy="strict-origin-when-cross-origin" loading="lazy"
   sandbox="allow-scripts allow-same-origin allow-presentation"></iframe>
} @else if(selected()?.kind==='direct' && selected()?.url;as url) {
  <video class="video" controls playsinline [src]="url">Tu navegador no admite video HTML5.</video>
} @else {
  <div class="player-placeholder"><h2>Video no disponible</h2>
    <p>No hay fuentes autorizadas disponibles para este episodio.</p></div>
}
<p class="notice">Los servidores externos pueden tener sus propias condiciones y restricciones.</p>
<div class="ad-slot">ESPACIO PUBLICITARIO · Desactivado</div>
</section>`})
export class WatchComponent implements OnInit {
  private route=inject(ActivatedRoute);private api=inject(CatalogService);
  private sanitizer=inject(DomSanitizer);
  slug='';number=1;sources=signal<VideoSource[]>([]);selected=signal<VideoSource|null>(null);
  safeEmbed=signal<SafeResourceUrl|null>(null);
  ngOnInit(){
    this.slug=this.route.snapshot.paramMap.get('slug')||'';
    this.number=Number(this.route.snapshot.paramMap.get('number')||1);
    this.api.sources(this.slug,this.number).subscribe({
      next:items=>{this.sources.set(items);if(items.length)this.selectSource(items[0]);},
      error:()=>this.sources.set([])
    });
  }
  selectSource(source:VideoSource){
    this.selected.set(source);this.safeEmbed.set(null);
    if(source.kind==='embed'){
      // The backend only exposes sources from configured, explicitly trusted hosts.
      this.safeEmbed.set(this.sanitizer.bypassSecurityTrustResourceUrl(source.url));
    }
  }
}
@Component({selector:'app-favorites',standalone:true,imports:[RouterLink],template:`
<section class="shell content"><h1>Mis favoritos</h1><p>La lista se guarda en este navegador.</p><div class="cards">@for(a of anime();track a.id){<a class="anime-card" [routerLink]="['/anime',a.slug]"><div class="poster">♥</div><div class="card-info">{{a.title}}</div></a>} @empty {<p>No tienes favoritos. <a routerLink="/catalogo">Explorar catálogo</a></p>}</div></section>
`})
export class FavoritesComponent implements OnInit {
  private api=inject(CatalogService);anime=signal<Anime[]>([]);
  ngOnInit(){this.api.search().subscribe({next:r=>this.anime.set(r.items.filter(a=>this.api.favorites().includes(a.id)))});}
}
@Component({selector:'app-admin',standalone:true,imports:[CommonModule],template:`
<section class="shell content"><h1>Administración</h1><p>La escritura está protegida por una clave de administrador que solo debe configurarse en el servidor. No almacenes secretos en el frontend.</p><p>El API ofrece endpoints protegidos para crear series, episodios y administrar posiciones de publicidad. El panel de edición visual se incorporará cuando exista un sistema de login seguro.</p><p class="error">No hay anuncios reales habilitados y no se permite cargar videos sin derechos de distribución.</p></section>
`})
export class AdminComponent {}
export const routes: Routes=[
  {path:'',component:CatalogComponent,title:'Nébula Anime — Inicio'},
  {path:'catalogo',component:CatalogComponent,title:'Catálogo — Nébula Anime'},
  {path:'anime/:slug',component:DetailComponent,title:'Serie — Nébula Anime'},
  {path:'ver/:slug/:number',component:WatchComponent,title:'Reproductor — Nébula Anime'},
  {path:'favoritos',component:FavoritesComponent,title:'Favoritos — Nébula Anime'},
  {path:'admin',component:AdminComponent,title:'Administración — Nébula Anime'},
  {path:'**',redirectTo:''}
];
