# Notification System - Ant Design

## Overview

The notification system uses Ant Design's built-in notification API wrapped in a React Context for easy access throughout the application.

## Setup

The `NotificationProvider` is already configured in `App.tsx` and wraps the entire application.

## Usage

### Import the Hook

```typescript
import { useNotification } from '../contexts/NotificationContext';
```

### Use in Your Component

```typescript
export default function YourComponent() {
  const notify = useNotification();

  // Success notification
  notify.success('Title', 'Description message');

  // Error notification
  notify.error('Error Title', 'Error description');

  // Warning notification
  notify.warning('Warning Title', 'Warning message');

  // Info notification
  notify.info('Info Title', 'Information message');
}
```

## API Reference

### `useNotification()`

Returns an object with the following methods:

#### `success(message: string, description?: string)`
Displays a success notification (green).

#### `error(message: string, description?: string)`
Displays an error notification (red).

#### `warning(message: string, description?: string)`
Displays a warning notification (orange).

#### `info(message: string, description?: string)`
Displays an info notification (blue).

## Configuration

All notifications:
- Appear in the **top-right** corner
- Auto-dismiss after **4.5 seconds** (Ant Design default)
- Show a close button
- Support both title and description
- Can be stacked (multiple notifications)

## Examples

### Simple Success Message

```typescript
notify.success('Saved!', 'Your changes have been saved successfully.');
```

### Error with Details

```typescript
notify.error('Login Failed', 'Invalid email or password. Please try again.');
```

### Without Description

```typescript
notify.success('Done!');
notify.error('Something went wrong');
```

### In Async Operations

```typescript
const handleSubmit = async () => {
  try {
    await someApiCall();
    notify.success('Success', 'Operation completed successfully');
  } catch (error) {
    notify.error('Failed', error.message);
  }
};
```

## Customization

To customize the notification appearance or behavior, modify the `NotificationContext.tsx` file:

```typescript
notification.success({
  message,
  description,
  placement: 'topRight',  // topLeft, topRight, bottomLeft, bottomRight, top, bottom
  duration: 4.5,           // seconds (0 = never auto-close)
  className: 'custom-class',
  style: { /* custom styles */ },
});
```

## Global Configuration

To change defaults for all notifications, update the ConfigProvider in `App.tsx`:

```typescript
<ConfigProvider
  theme={{
    // ... existing theme
  }}
>
  {/* This is where you could add notification config if needed */}
</ConfigProvider>
```

---

**Note:** The notification system leverages Ant Design's `App` component which provides the notification instance via hooks. This is the recommended approach for Ant Design v5+.

