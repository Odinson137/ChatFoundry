# ChatFoundry Design System

## Шрифт
- **Семейство:** Inter (Google Fonts), fallback: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif
- **Моноширинный:** 'Courier New', monospace (для тегов технологий, названий сервисов)
- **Сглаживание:** `-webkit-font-smoothing: antialiased`, `-moz-osx-font-smoothing: grayscale`

## Размеры шрифта

| Элемент | Размер | Вес | Letter-spacing |
|---------|--------|-----|----------------|
| Hero заголовок (h1) | 3.25rem | 800 | -0.035em |
| Секция заголовок (h2) | 2rem | 800 | -0.03em |
| Карточка заголовок (h3) | 1.05rem | 700 | — |
| Основной текст | 0.875rem | 400 | — |
| Подзаголовок / описание | 1.05–1.15rem | 400 | — |
| Бейдж / тег | 0.8rem | 600 | 0.02em |
| Кнопка | 0.875–0.95rem | 600 | — |

## Цветовая палитра

### Основные цвета

| Название | HEX | Использование |
|----------|-----|---------------|
| Slate 900 | `#0f172a` | Основной текст, заголовки |
| Slate 500 | `#64748b` | Вторичный текст, описания |
| Slate 400 | `#94a3b8` | Приглушённый текст, подписи |
| Slate 200 | `#e2e8f0` | Границы, разделители |
| Slate 100 | `#f1f5f9` | Лёгкие границы, фон карточек |
| Slate 50 | `#f8fafc` | Фон секций (чередование) |
| White | `#ffffff` | Основной фон |

### Акцентные цвета (градиент)

| Название | HEX | Использование |
|----------|-----|---------------|
| Indigo 500 | `#6366f1` | Основной акцент, кнопки, ссылки |
| Violet 500 | `#8b5cf6` | Конец градиента |
| Indigo 600 | `#4f46e5` | Hover-состояние |
| Violet 600 | `#7c3aed` | Hover-состояние градиента |
| Indigo 100 | `#eef2ff` | Фон активных навигационных элементов, бейджей |
| Indigo 300 | `#818cf8` | Focus ring |

### Основной градиент

```css
background: linear-gradient(135deg, #6366f1, #8b5cf6);
```

### Цвета для feature-иконок

| Цвет | Фон | Иконка | Назначение |
|------|-----|--------|------------|
| Indigo | `#eef2ff` | `#6366f1` | Конструктор |
| Violet | `#faf5ff` | `#8b5cf6` | AI |
| Emerald | `#ecfdf5` | `#10b981` | Мессенджеры |
| Orange | `#fff7ed` | `#f97316` | Self-hosted |
| Red | `#fef2f2` | `#ef4444` | Производительность |
| Sky | `#f0f9ff` | `#0ea5e9` | Безопасность |

## Скругления (border-radius)

| Элемент | Значение |
|---------|----------|
| Кнопки | 8–10px |
| Карточки | 12–16px |
| Навигационные элементы | 6px |
| Иконки (контейнер) | 12px |
| Бейджи / пилюли | 100px |
| Логотип (SVG rect) | 7px |

## Тени (box-shadow)

```css
/* Кнопка — состояние покоя */
box-shadow: 0 1px 3px rgba(99, 102, 241, 0.3);

/* Кнопка — hover */
box-shadow: 0 4px 12px rgba(99, 102, 241, 0.35);

/* Карточка — hover */
box-shadow: 0 4px 16px rgba(0, 0, 0, 0.04);

/* Hero-визуал (контейнер workflow) */
box-shadow: 0 4px 24px rgba(0, 0, 0, 0.06), 0 1px 4px rgba(0, 0, 0, 0.04);

/* Featured pricing-карточка */
box-shadow: 0 4px 20px rgba(99, 102, 241, 0.12);
```

## Кнопки

### Primary (градиентная)

```css
padding: 12px 28px;
background: linear-gradient(135deg, #6366f1, #8b5cf6);
color: #ffffff;
font-weight: 600;
border-radius: 10px;
/* hover: translateY(-2px), усиленная тень */
```

### Secondary (с обводкой)

```css
padding: 12px 28px;
background: #ffffff;
color: #0f172a;
border: 1px solid #e2e8f0;
border-radius: 10px;
/* hover: background #f8fafc, border #cbd5e1 */
```

## Header

- Высота: 64px
- `position: sticky; top: 0;`
- Frosted glass: `background: rgba(255, 255, 255, 0.92); backdrop-filter: blur(12px);`
- Нижняя граница: `1px solid #f1f5f9`
- Контейнер: `max-width: 1200px`, центрирован

## Сетка контента

- Максимальная ширина: **1200px**, центрирование через `margin: 0 auto`
- Padding секций: `5rem 2rem` (desktop), `3.5rem 1.5rem` (mobile)
- Feature-карточки: grid 3 колонки → 2 → 1 (responsive)
- Pricing-карточки: grid 3 колонки → 2 → 1
- Архитектура: grid 3 колонки → 2 → 1

## Навигация (pill-style)

```css
padding: 6px 14px;
font-size: 0.875rem;
font-weight: 500;
color: #64748b;          /* обычное */
color: #6366f1;          /* active */
background: #eef2ff;     /* active */
border-radius: 6px;
```

## Анимации / Transitions

- Кнопки: `transition: all 0.2s ease;` + `transform: translateY(-1px)` на hover
- Навигация: `transition: color 0.15s, background 0.15s;`
- Карточки: `transition: border-color 0.2s, box-shadow 0.2s;`

## Паттерн чередования секций

Белый фон (`#ffffff`) → Светло-серый (`#f8fafc`) → Белый → Серый → Тёмный CTA (`#0f172a`)

## CTA-секция (тёмная)

```css
background: linear-gradient(135deg, #0f172a, #1e293b);
/* Текст: #ffffff для заголовка, #94a3b8 для подзаголовка */
```

## Workflow-нодки (визуализация)

| Тип | Фон | Текст | Граница |
|-----|-----|-------|---------|
| Start | `#ecfdf5` | `#059669` | `#a7f3d0` |
| Message | `#eef2ff` | `#4f46e5` | `#c7d2fe` |
| AI Generate | `#faf5ff` | `#7c3aed` | `#ddd6fe` |
| Condition | `#fff7ed` | `#ea580c` | `#fed7aa` |

## Responsive breakpoints

| Breakpoint | Что меняется |
|------------|-------------|
| 1024px | Grid 3→2 колонки |
| 768px | Grid 2→1, steps вертикально, hero уменьшается |
| 480px | Мелкие шрифты, компактные кнопки |
