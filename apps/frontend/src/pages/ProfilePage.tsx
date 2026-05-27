import { useState, type FormEvent } from 'react';
import { api } from '../lib/api';
import { useAuth } from '../contexts/AuthContext';

export function ProfilePage() {
  const { user } = useAuth();
  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);
  const [loading, setLoading] = useState(false);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setSuccess(false);

    if (newPassword !== confirmPassword) {
      setError('A nova senha e a confirmação não coincidem.');
      return;
    }

    setLoading(true);
    try {
      await api.put('/auth/change-password', {
        currentPassword,
        newPassword,
        confirmPassword,
      });
      setSuccess(true);
      setCurrentPassword('');
      setNewPassword('');
      setConfirmPassword('');
    } catch (err: unknown) {
      const msg =
        (err as { response?: { data?: { error?: string } } })?.response?.data?.error ??
        'Erro ao alterar a senha.';
      setError(msg);
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="max-w-md">
      <h1 className="text-2xl font-bold text-ink mb-1">Meu Perfil</h1>
      <p className="text-sm text-ink-muted mb-8">
        Usuário: <span className="font-medium text-ink">{user?.username}</span>
      </p>

      <div className="bg-white rounded-2xl border border-gray-200 shadow-sm p-6">
        <h2 className="text-base font-semibold text-ink mb-5">Alterar Senha</h2>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label htmlFor="currentPassword" className="block text-sm font-medium text-ink-soft mb-1">
              Senha atual
            </label>
            <input
              id="currentPassword"
              type="password"
              required
              value={currentPassword}
              onChange={e => setCurrentPassword(e.target.value)}
              className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm text-ink focus:outline-none focus:ring-2 focus:ring-brand-500 focus:border-transparent transition"
              placeholder="••••••••"
            />
          </div>

          <div>
            <label htmlFor="newPassword" className="block text-sm font-medium text-ink-soft mb-1">
              Nova senha
            </label>
            <input
              id="newPassword"
              type="password"
              required
              value={newPassword}
              onChange={e => setNewPassword(e.target.value)}
              className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm text-ink focus:outline-none focus:ring-2 focus:ring-brand-500 focus:border-transparent transition"
              placeholder="••••••••"
            />
          </div>

          <div>
            <label htmlFor="confirmPassword" className="block text-sm font-medium text-ink-soft mb-1">
              Confirmar nova senha
            </label>
            <input
              id="confirmPassword"
              type="password"
              required
              value={confirmPassword}
              onChange={e => setConfirmPassword(e.target.value)}
              className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm text-ink focus:outline-none focus:ring-2 focus:ring-brand-500 focus:border-transparent transition"
              placeholder="••••••••"
            />
          </div>

          {error && (
            <p className="text-sm text-red-500 bg-red-50 rounded-lg px-3 py-2">{error}</p>
          )}

          {success && (
            <p className="text-sm text-green-700 bg-green-50 rounded-lg px-3 py-2">
              Senha alterada com sucesso!
            </p>
          )}

          <button
            type="submit"
            disabled={loading}
            className="w-full bg-brand-600 hover:bg-brand-500 disabled:opacity-60 text-white font-medium rounded-lg py-2.5 text-sm transition focus:outline-none focus:ring-2 focus:ring-brand-500 focus:ring-offset-2"
          >
            {loading ? 'Salvando...' : 'Alterar senha'}
          </button>
        </form>
      </div>
    </div>
  );
}
