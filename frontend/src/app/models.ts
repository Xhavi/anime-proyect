export interface Anime {
  id: number;
  slug: string;
  title: string;
  description: string;
  genre: string;
  year: number;
  episodeCount: number;
  coverUrl: string | null;
}
export interface Episode {
  id: number;
  number: number;
  title: string;
  videoUrl: string | null;
  isPublished: boolean;
}
export interface AdPlacement { id: number; code: string; enabled: boolean; network: string; label: string; }
export interface PagedResult<T> { items: T[]; total: number; page: number; pageSize: number; }

export interface VideoSource {
  id: number;
  provider: string;
  kind: 'embed' | 'direct';
  url: string;
  quality: string;
}
