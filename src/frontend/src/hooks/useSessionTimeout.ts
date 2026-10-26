import { useEffect, useRef, useState, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuthStore } from '@/stores/authStore';
import { api } from '@/services/api';

const TIMEOUT_MS = 5 * 60 * 1000;       // 5 minutos de inatividade
const WARNING_MS = 60 * 1000;            // exibe aviso 1 min antes de expirar
const ACTIVITY_SYNC_INTERVAL_MS = 30 * 1000;

const ACTIVITY_EVENTS = [
  'mousemove',
  'mousedown',
  'keydown',
  'touchstart',
  'scroll',
  'click',
] as const;

export function useSessionTimeout() {
  const { isAuthenticated, logout } = useAuthStore();
  const navigate = useNavigate();
  const timeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const warningRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const [showWarning, setShowWarning] = useState(false);
  const [remainingSeconds, setRemainingSeconds] = useState(60);
  const countdownRef = useRef<ReturnType<typeof setInterval> | null>(null);
  const lastActivitySyncRef = useRef(Date.now());

  const clearAllTimers = useCallback(() => {
    if (timeoutRef.current) clearTimeout(timeoutRef.current);
    if (warningRef.current) clearTimeout(warningRef.current);
    if (countdownRef.current) clearInterval(countdownRef.current);
  }, []);

  const handleLogout = useCallback(() => {
    clearAllTimers();
    setShowWarning(false);
    logout();
    navigate('/login', { replace: true });
  }, [clearAllTimers, logout, navigate]);

  const startCountdown = useCallback(() => {
    setRemainingSeconds(60);
    setShowWarning(true);
    if (countdownRef.current) clearInterval(countdownRef.current);
    countdownRef.current = setInterval(() => {
      setRemainingSeconds((prev) => {
        if (prev <= 1) {
          if (countdownRef.current) clearInterval(countdownRef.current);
          return 0;
        }
        return prev - 1;
      });
    }, 1000);
  }, []);

  const resetTimers = useCallback(() => {
    clearAllTimers();
    setShowWarning(false);
    setRemainingSeconds(60);

    warningRef.current = setTimeout(() => {
      startCountdown();
    }, TIMEOUT_MS - WARNING_MS);

    timeoutRef.current = setTimeout(() => {
      handleLogout();
    }, TIMEOUT_MS);
  }, [clearAllTimers, startCountdown, handleLogout]);

  // Permite ao usuário manter a sessão ao interagir com o modal
  const extendSession = useCallback(() => {
    resetTimers();
  }, [resetTimers]);

  useEffect(() => {
    if (!isAuthenticated) return;

    lastActivitySyncRef.current = Date.now();
    resetTimers();

    const handleActivity = () => {
      resetTimers();

      const now = Date.now();
      if (now - lastActivitySyncRef.current < ACTIVITY_SYNC_INTERVAL_MS) return;

      lastActivitySyncRef.current = now;
      void api.post('/auth/activity').catch(() => {
        handleLogout();
      });
    };

    ACTIVITY_EVENTS.forEach((evt) => window.addEventListener(evt, handleActivity, { passive: true }));

    return () => {
      clearAllTimers();
      ACTIVITY_EVENTS.forEach((evt) => window.removeEventListener(evt, handleActivity));
    };
  }, [isAuthenticated, resetTimers, clearAllTimers, handleLogout]);

  return { showWarning, remainingSeconds, extendSession, handleLogout };
}
