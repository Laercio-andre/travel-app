import { ApiList } from '../models/travel.models';

export function unwrapList<T>(response: T[] | ApiList<T>): T[] {
  if (Array.isArray(response)) {
    return response;
  }

  return response.items ?? response.data ?? [];
}

export function downloadBlob(blob: Blob, filename: string): void {
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement('a');
  anchor.href = url;
  anchor.download = filename;
  anchor.click();
  URL.revokeObjectURL(url);
}
