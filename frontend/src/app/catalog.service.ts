import { Injectable, inject, signal } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Anime, Episode, PagedResult, AdPlacement, VideoSource } from './models';
@Injectable({providedIn:'root'})
export class CatalogService {
  private http = inject(HttpClient);
  readonly baseUrl = (typeof window !== 'undefined' && (window as Window & { __NEBULA_API__?: string }).__NEBULA_API__) || 'http://localhost:5050';
  readonly favorites = signal<number[]>(this.loadFavorites());
  search(query = '', genre = '', page = 1): Observable<PagedResult<Anime>> {
    let params = new HttpParams().set('page',page).set('pageSize',20);
    if(query) params=params.set('q',query);
    if(genre) params=params.set('genre',genre);
    return this.http.get<PagedResult<Anime>>(`${this.baseUrl}/api/anime`,{params});
  }
  anime(slug:string): Observable<Anime> { return this.http.get<Anime>(`${this.baseUrl}/api/anime/${encodeURIComponent(slug)}`); }
  episodes(slug:string): Observable<Episode[]> { return this.http.get<Episode[]>(`${this.baseUrl}/api/anime/${encodeURIComponent(slug)}/episodes`); }
  sources(slug:string,number:number): Observable<VideoSource[]> {
    return this.http.get<VideoSource[]>(`${this.baseUrl}/api/anime/${encodeURIComponent(slug)}/episodes/${number}/sources`);
  }
  placements(): Observable<AdPlacement[]> { return this.http.get<AdPlacement[]>(`${this.baseUrl}/api/ads`); }
  toggleFavorite(id:number):void {
    this.favorites.update(ids => ids.includes(id) ? ids.filter(x=>x!==id) : [...ids,id]);
    try { localStorage.setItem('nebula-favorites',JSON.stringify(this.favorites())); } catch { /* storage optional */ }
  }
  private loadFavorites():number[] {
    try {
      const ids=JSON.parse(localStorage.getItem('nebula-favorites') || '[]');
      return Array.isArray(ids) ? ids.filter((v):v is number=>typeof v==='number' && Number.isInteger(v)) : [];
    } catch { return []; }
  }
}
