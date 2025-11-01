# 🌙 Dark Mode Implementation Guide

**Version**: 1.0  
**Last Updated**: October 29, 2025  
**Status**: ✅ Production Ready

---

## 📋 Table of Contents

1. [Overview](#overview)
2. [How Dark Mode Works](#how-dark-mode-works)
3. [CSS Variables](#css-variables)
4. [Component Integration](#component-integration)
5. [Button Styles](#button-styles)
6. [Usage Guidelines](#usage-guidelines)
7. [Best Practices](#best-practices)
8. [Troubleshooting](#troubleshooting)

---

## 🎯 Overview

SmallHR uses a **context-based dark mode** implementation with CSS variables for theming. The system automatically adapts all components, maintaining consistency across the entire application.

### Key Features
- ✅ Context-based state management (`ThemeContext`)
- ✅ Persistent user preference (localStorage)
- ✅ CSS variable-driven theming
- ✅ Automatic Ant Design component styling
- ✅ Smooth transitions between modes

---

## ⚙️ How Dark Mode Works

### 1. Theme Context (`src/contexts/ThemeContext.tsx`)

The `ThemeContext` manages dark mode state:

```typescript
// State management
const [isDarkMode, setIsDarkMode] = useState(() => {
  const saved = localStorage.getItem('darkMode');
  return saved === 'true';
});

// Applies .dark-mode class to <html> element
useEffect(() => {
  if (isDarkMode) {
    document.documentElement.classList.add('dark-mode');
  } else {
    document.documentElement.classList.remove('dark-mode');
  }
}, [isDarkMode]);
```

### 2. CSS Variables (`src/index.css`)

All colors and styles are defined as CSS variables in `:root` and `.dark-mode`:

```css
:root {
  /* Light mode variables */
  --color-text-primary: #0F172A;
  --gradient-primary: linear-gradient(135deg, #667EEA 0%, #764BA2 100%);
}

.dark-mode {
  /* Dark mode overrides */
  --color-text-primary: #F1F5F9;
  --gradient-primary: linear-gradient(135deg, #818CF8 0%, #A78BFA 100%);
}
```

### 3. Toggle Button (`src/components/Layout/Header.tsx`)

Located in the header, next to the time display:

```typescript
const { isDarkMode, toggleDarkMode } = useTheme();

<div onClick={toggleDarkMode}>
  {isDarkMode ? <BulbFilled /> : <BulbOutlined />}
</div>
```

---

## 🎨 CSS Variables

### Color System

#### Text Colors
```css
/* Light Mode */
--color-text-primary: #0F172A      /* Main text */
--color-text-secondary: #64748B    /* Secondary text */
--color-text-tertiary: #94A3B8     /* Muted text */

/* Dark Mode */
--color-text-primary: #F1F5F9      /* Main text */
--color-text-secondary: #CBD5E1    /* Secondary text */
--color-text-tertiary: #94A3B8     /* Muted text */
```

#### Brand Colors
```css
/* Light Mode */
--color-primary: #4F46E5           /* Indigo */
--color-accent: #06B6D4            /* Cyan */
--color-success: #10B981           /* Green */
--color-warning: #F59E0B           /* Orange */
--color-error: #EF4444             /* Red */

/* Dark Mode */
--color-primary: #818CF8           /* Lighter indigo */
--color-accent: #22D3EE            /* Lighter cyan */
--color-success: #34D399           /* Lighter green */
--color-warning: #FBBF24           /* Lighter orange */
--color-error: #F87171             /* Lighter red */
```

#### Background Colors
```css
/* Light Mode */
--color-background: #F8FAFC
--color-surface: #FFFFFF
--glass-background: rgba(255, 255, 255, 0.8)

/* Dark Mode */
--color-background: #0F172A
--color-surface: #1E293B
--glass-background: rgba(30, 41, 59, 0.8)
```

### Gradient System

#### Available Gradients
```css
/* Light Mode */
--gradient-primary: linear-gradient(135deg, #667EEA 0%, #764BA2 100%)
--gradient-accent: linear-gradient(135deg, #06B6D4 0%, #3B82F6 100%)
--gradient-success: linear-gradient(135deg, #10B981 0%, #059669 100%)
--gradient-warning: linear-gradient(135deg, #F59E0B 0%, #D97706 100%)
--gradient-error: linear-gradient(135deg, #EF4444 0%, #DC2626 100%)
--gradient-info: linear-gradient(135deg, #3B82F6 0%, #2563EB 100%)

/* Dark Mode */
--gradient-primary: linear-gradient(135deg, #818CF8 0%, #A78BFA 100%)
--gradient-accent: linear-gradient(135deg, #22D3EE 0%, #60A5FA 100%)
--gradient-success: linear-gradient(135deg, #34D399 0%, #10B981 100%)
--gradient-warning: linear-gradient(135deg, #FBBF24 0%, #F59E0B 100%)
--gradient-error: linear-gradient(135deg, #F87171 0%, #EF4444 100%)
--gradient-info: linear-gradient(135deg, #60A5FA 0%, #3B82F6 100%)
```

### Button Variables

```css
/* Consistent across light and dark modes */
--button-height: 38px
--button-radius: 10px
--button-font-family: 'Inter', sans-serif
--button-font-weight: 500

/* Shadows (adjust per mode) */
--button-shadow-sm: 0 2px 4px rgba(0, 0, 0, 0.06)
--button-shadow-md: 0 2px 6px rgba(0, 0, 0, 0.1)
--button-shadow-primary: 0 2px 6px rgba(79, 70, 229, 0.3)
--button-shadow-success: 0 2px 6px rgba(16, 185, 129, 0.2)
--button-shadow-warning: 0 2px 6px rgba(245, 158, 11, 0.3)
--button-shadow-error: 0 2px 6px rgba(239, 68, 68, 0.3)
```

---

## 🔧 Component Integration

### Using CSS Variables in Components

#### ✅ Correct Usage
```typescript
// Use CSS variables for dynamic theming
<div style={{
  color: 'var(--color-text-primary)',
  background: 'var(--gradient-success)',
  borderColor: 'var(--glass-border)',
}}>
  Content
</div>
```

#### ❌ Incorrect Usage
```typescript
// Never hardcode colors
<div style={{
  color: '#1E293B',              // Bad: won't adapt to dark mode
  background: 'linear-gradient(135deg, #10B981 0%, #059669 100%)', // Bad
  borderColor: '#E2E8F0',        // Bad
}}>
  Content
</div>
```

### Common Patterns

#### Text Elements
```typescript
<span style={{
  color: 'var(--color-text-primary, #1E293B)',  // Fallback included
  fontFamily: 'var(--button-font-family)',
}}>
  Text
</span>
```

#### Backgrounds
```typescript
<div style={{
  background: 'var(--glass-background)',
  backdropFilter: 'blur(8px)',
  borderRadius: 'var(--radius-lg)',
}}>
  Card Content
</div>
```

#### Borders
```typescript
<div style={{
  border: '1px solid var(--glass-border)',
  borderRadius: 'var(--button-radius)',
}}>
  Content
</div>
```

---

## 🎯 Button Styles

### Standard Button Patterns

#### Primary Action Button
```typescript
<Button
  type="primary"
  style={{
    borderRadius: 'var(--button-radius)',
    height: 'var(--button-height)',
    background: 'var(--gradient-primary)',
    fontFamily: 'var(--button-font-family)',
    fontWeight: 'var(--button-font-weight)',
    boxShadow: 'var(--button-shadow-primary)',
    border: 'none',
  }}
>
  Primary Action
</Button>
```

#### Success Button
```typescript
<Button
  type="primary"
  style={{
    borderRadius: 'var(--button-radius)',
    height: 'var(--button-height)',
    background: 'var(--gradient-success)',
    fontFamily: 'var(--button-font-family)',
    fontWeight: 'var(--button-font-weight)',
    boxShadow: 'var(--button-shadow-success)',
    border: 'none',
  }}
>
  Success Action
</Button>
```

#### Secondary Button
```typescript
<Button
  style={{
    borderRadius: 'var(--button-radius)',
    height: 'var(--button-height)',
    background: 'var(--glass-background)',
    backdropFilter: 'blur(8px)',
    borderColor: 'var(--glass-border)',
    color: 'var(--color-text-secondary)',
    fontFamily: 'var(--button-font-family)',
    fontWeight: 'var(--button-font-weight)',
    boxShadow: 'var(--button-shadow-sm)',
  }}
>
  Secondary Action
</Button>
```

---

## 📘 Usage Guidelines

### When Creating New Components

1. **Always use CSS variables** for colors, backgrounds, and borders
2. **Include fallback values** for better browser compatibility
3. **Test in both modes** before committing
4. **Use semantic variable names** (e.g., `--color-text-primary` not `--dark-gray`)

### Dark Mode Checklist

When implementing a new feature, ensure:

- [ ] All text colors use CSS variables
- [ ] All backgrounds use CSS variables or gradients
- [ ] All borders use `var(--glass-border)`
- [ ] Buttons use standard button variables
- [ ] Icons are visible in both modes
- [ ] Hover states work in both modes
- [ ] Shadows are appropriate for both modes
- [ ] Component tested in light mode
- [ ] Component tested in dark mode

---

## ✨ Best Practices

### 1. Consistency
Always use the same variable for the same purpose:
```typescript
// Good
color: 'var(--color-text-primary)'

// Bad - mixing variables
color: '#1E293B'  // in one place
color: 'var(--color-text-primary)'  // in another
```

### 2. Gradients Over Solid Colors
For branded elements, prefer gradients:
```typescript
// Good - uses gradient
background: 'var(--gradient-primary)'

// Acceptable but less visual impact
background: 'var(--color-primary)'
```

### 3. Glass Morphism
Use glass effects for cards and containers:
```typescript
background: 'var(--glass-background)',
backdropFilter: 'blur(8px)',
border: '1px solid var(--glass-border)',
```

### 4. Typography
Always use consistent font properties:
```typescript
fontFamily: 'var(--button-font-family)',  // or 'Inter, sans-serif'
fontWeight: 'var(--button-font-weight)',  // or 500
```

### 5. Fallback Values
Include fallback values for critical styling:
```typescript
color: 'var(--color-text-primary, #1E293B)'
```

---

## 🐛 Troubleshooting

### Common Issues

#### Issue: Component not changing in dark mode
**Solution**: Check if hardcoded colors are used instead of CSS variables
```typescript
// Before (wrong)
color: '#64748B'

// After (correct)
color: 'var(--color-text-secondary)'
```

#### Issue: White text on white background
**Solution**: Ensure background uses proper variable
```typescript
// Before
background: '#FFFFFF'

// After
background: 'var(--glass-background)'
```

#### Issue: Ant Design component not themed
**Solution**: Check if dark mode CSS rule exists in `index.css`
```css
.dark-mode .ant-component {
  background: var(--color-surface) !important;
  color: var(--color-text-primary) !important;
}
```

#### Issue: Button gradient not showing in dark mode
**Solution**: Ensure gradient variable is defined in `.dark-mode` section
```css
.dark-mode {
  --gradient-primary: linear-gradient(135deg, #818CF8 0%, #A78BFA 100%);
}
```

---

## 📚 Reference

### Files to Check
- **Theme Context**: `src/contexts/ThemeContext.tsx`
- **CSS Variables**: `src/index.css` (lines 1-110 for :root, 70-106 for .dark-mode)
- **Toggle Button**: `src/components/Layout/Header.tsx`
- **App Wrapper**: `src/App.tsx`

### Related Documentation
- [UI Design Guide](./UI_DESIGN_GUIDE.md)
- Component-specific styling
- Ant Design theme customization

---

## 🎨 Quick Reference Card

| Element | Variable | Usage |
|---------|----------|-------|
| **Primary Text** | `var(--color-text-primary)` | Headings, important text |
| **Secondary Text** | `var(--color-text-secondary)` | Body text, labels |
| **Primary Gradient** | `var(--gradient-primary)` | Main CTA buttons |
| **Success Gradient** | `var(--gradient-success)` | Success actions |
| **Glass Background** | `var(--glass-background)` | Cards, modals |
| **Glass Border** | `var(--glass-border)` | All borders |
| **Button Height** | `var(--button-height)` | All buttons |
| **Button Radius** | `var(--button-radius)` | All buttons |

---

**Remember**: When in doubt, check existing components for patterns! 🚀

