import { useEffect } from "react";

// Sets the document title with " | متابعة" suffix
export function usePageTitle(title: string) {
  useEffect(() => {
    document.title = `${title} | متابعة`;
    return () => {
      document.title = "متابعة";
    };
  }, [title]);
}
