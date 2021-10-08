import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class LocalStorageService {

  constructor() { }

  public getValue<T>(key: string) : T | null {
    const rawVal = localStorage.getItem(key);
    if (!rawVal) {
      return null;
    }
    return JSON.parse(rawVal) as T;
  }

  public setValue<T>(key: string, val: T): void {
    localStorage.setItem(key, JSON.stringify(val));
  }

  public clearAll(): void {
    localStorage.clear();
  }
}
