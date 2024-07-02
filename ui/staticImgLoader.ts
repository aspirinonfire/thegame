'use client'

interface Loader {
  src: string,
  width: number,
  quality?: number
}

export default function StaticImageLoader({src}: Loader) {
  return src;
}