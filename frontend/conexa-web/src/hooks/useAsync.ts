import { useEffect, useState } from 'react';

/** Carrega dados assíncronos; reexecuta quando `deps` mudam ou ao chamar `reload()`. */
export function useAsync<T>(fn: () => Promise<T>, deps: unknown[]) {
  const [state, setState] = useState<{ data?: T; error?: Error; loading: boolean }>({ loading: true });
  const [tick, setTick] = useState(0);

  useEffect(() => {
    let cancelled = false;
    setState((s) => ({ data: s.data, loading: true }));
    fn()
      .then((data) => { if (!cancelled) setState({ data, loading: false }); })
      .catch((error: Error) => { if (!cancelled) setState({ error, loading: false }); });
    return () => { cancelled = true; };
  }, [...deps, tick]);

  return { ...state, reload: () => setTick((t) => t + 1) };
}
