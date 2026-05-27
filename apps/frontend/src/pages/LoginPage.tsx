import { useState, type FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { api } from '../lib/api';
import { useAuth } from '../contexts/AuthContext';

export function LoginPage() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setLoading(true);
    try {
      const { data } = await api.post<{ token: string; username: string; expiresAt: string }>(
        '/auth/login',
        { username, password },
      );
      login(data.username, data.token, data.expiresAt);
      navigate('/', { replace: true });
    } catch {
      setError('Usuário ou senha inválidos.');
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="min-h-screen bg-white flex items-center justify-center px-4">
      <div className="w-full max-w-sm">
        {/* Logo / wordmark */}
        <div className="mb-8 text-center">
          <div className="inline-flex flex-col items-center gap-3 mb-2">
            <img src="/logo.jpeg" alt="Light & Vibe" className="h-20 w-auto max-w-[200px] object-contain" />
          </div>
          <p className="text-sm text-ink-muted">Painel de Controle</p>
        </div>

        <div className="bg-white rounded-2xl shadow-sm border border-gray-200 p-8">
          <h1 className="text-xl font-semibold text-ink mb-6">Entrar</h1>

          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <label htmlFor="username" className="block text-sm font-medium text-ink-soft mb-1">
                Usuário
              </label>
              <input
                id="username"
                type="text"
                autoComplete="username"
                required
                value={username}
                onChange={e => setUsername(e.target.value)}
                className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm text-ink placeholder:text-ink-muted focus:outline-none focus:ring-2 focus:ring-brand-500 focus:border-transparent transition"
                placeholder="seu usuário"
              />
            </div>

            <div>
              <label htmlFor="password" className="block text-sm font-medium text-ink-soft mb-1">
                Senha
              </label>
              <input
                id="password"
                type="password"
                autoComplete="current-password"
                required
                value={password}
                onChange={e => setPassword(e.target.value)}
                className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm text-ink placeholder:text-ink-muted focus:outline-none focus:ring-2 focus:ring-brand-500 focus:border-transparent transition"
                placeholder="••••••••"
              />
            </div>

            {error && (
              <p className="text-sm text-red-500 bg-red-50 rounded-lg px-3 py-2">{error}</p>
            )}

            <button
              type="submit"
              disabled={loading}
              className="w-full bg-brand-600 hover:bg-brand-500 disabled:opacity-60 text-white font-medium rounded-lg py-2.5 text-sm transition focus:outline-none focus:ring-2 focus:ring-brand-500 focus:ring-offset-2"
            >
              {loading ? 'Entrando...' : 'Entrar'}
            </button>
          </form>
        </div>
      </div>
    </div>
  );
}
