import { useState, useMemo } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { Eye, EyeOff, LogIn } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Button, Input } from '@/components/ui';
import { useAuthStore, setRememberMe } from '@/stores/authStore';
import { authService } from '@/services/authService';
import toast from 'react-hot-toast';

export default function Login() {
  const navigate = useNavigate();
  const { setAuth } = useAuthStore();
  const [showPassword, setShowPassword] = useState(false);
  const [loading, setLoading] = useState(false);
  const { t, i18n } = useTranslation();

  const loginSchema = useMemo(
    () =>
      z.object({
        email: z.string().email(t('auth.emailInvalid')).min(1, t('auth.emailRequired')),
        password: z.string().min(1, t('auth.passwordRequired')),
        rememberMe: z.boolean().optional(),
      }),
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [i18n.language, t]
  );

  type LoginForm = z.infer<typeof loginSchema>;

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<LoginForm>({
    resolver: zodResolver(loginSchema),
  });

  const onSubmit = async (data: LoginForm) => {
    setLoading(true);

    try {
      const response = await authService.login({
        email: data.email,
        password: data.password,
      });

      setRememberMe(data.rememberMe === true);
      setAuth(
        response.user,
        response.accessToken,
        response.refreshToken
      );
      
      toast.success(t('auth.loginSuccess'));
      navigate('/dashboard');
    } catch (error: unknown) {
      const message = (error as { response?: { data?: { message?: string } } })?.response?.data?.message || t('auth.invalidCredentials');
      toast.error(message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="animate-fade-in">
      {/* Mobile Logo */}
      <div className="lg:hidden flex items-center justify-center mb-8">
        <img
          src="/logo2.png"
          alt="WP Manager"
          className="h-48 w-auto object-contain"
        />
      </div>

      {/* Form Header */}
      <div className="mb-8">
        <h2 className="text-2xl font-bold text-primary">{t('auth.welcome')}</h2>
        <p className="text-primary-500 mt-1">
          {t('auth.signInDescription')}
        </p>
      </div>

      {/* Form */}
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">
        <Input
          label={t('auth.email')}
          type="email"
          placeholder={t('auth.emailPlaceholder')}
          error={errors.email?.message}
          {...register('email')}
        />

        <div className="relative">
          <Input
            label={t('auth.password')}
            type={showPassword ? 'text' : 'password'}
            placeholder="••••••••"
            error={errors.password?.message}
            {...register('password')}
          />
          <button
            type="button"
            onClick={() => setShowPassword(!showPassword)}
            className="absolute right-3 top-9 text-primary-400 hover:text-primary-600"
          >
            {showPassword ? (
              <EyeOff className="w-5 h-5" />
            ) : (
              <Eye className="w-5 h-5" />
            )}
          </button>
        </div>

        <div className="flex items-center justify-between">
          <label className="flex items-center gap-2 cursor-pointer">
            <input
              type="checkbox"
              {...register('rememberMe')}
              className="w-4 h-4 rounded border-primary-300 text-accent focus:ring-accent"
            />
            <span className="text-sm text-primary-600">{t('auth.rememberMe')}</span>
          </label>
          <Link
            to="/forgot-password"
            className="text-sm text-accent hover:text-accent-700 font-medium"
          >
            {t('auth.forgotPassword')}
          </Link>
        </div>

        <Button
          type="submit"
          className="w-full"
          loading={loading}
          icon={<LogIn className="w-5 h-5" />}
        >
          {t('auth.signIn')}
        </Button>
      </form>

      <p className="mt-4 text-center text-xs text-primary-400">Version 0.1.3</p>
    </div>
  );
}
