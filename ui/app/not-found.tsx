"use client"
import { useRouter } from "next/navigation";
 
/**
 * Default 404/not found operation is redirect to root '/'
 */
export default function NotFound() {
  const router = useRouter();
  router.push('/');
}