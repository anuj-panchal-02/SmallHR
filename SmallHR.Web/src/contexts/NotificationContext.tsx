import { createContext, useContext, ReactNode } from 'react';
import { App } from 'antd';

interface NotificationContextType {
  success: (message: string, description?: string) => void;
  error: (message: string, description?: string) => void;
  warning: (message: string, description?: string) => void;
  info: (message: string, description?: string) => void;
}

const NotificationContext = createContext<NotificationContextType | undefined>(undefined);

export const NotificationProvider = ({ children }: { children: ReactNode }) => {
  const { notification } = App.useApp();

  const success = (message: string, description?: string) => {
    notification.success({
      message,
      description,
      placement: 'topRight',
    });
  };

  const error = (message: string, description?: string) => {
    notification.error({
      message,
      description,
      placement: 'topRight',
    });
  };

  const warning = (message: string, description?: string) => {
    notification.warning({
      message,
      description,
      placement: 'topRight',
    });
  };

  const info = (message: string, description?: string) => {
    notification.info({
      message,
      description,
      placement: 'topRight',
    });
  };

  const value: NotificationContextType = {
    success,
    error,
    warning,
    info,
  };

  return (
    <NotificationContext.Provider value={value}>
      {children}
    </NotificationContext.Provider>
  );
};

export const useNotification = () => {
  const context = useContext(NotificationContext);
  if (context === undefined) {
    throw new Error('useNotification must be used within a NotificationProvider');
  }
  return context;
};

