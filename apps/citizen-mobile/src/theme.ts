import { Platform, TextStyle, ViewStyle } from 'react-native';

// NetCAD marka paleti — lacivert + mavi + cyan (web ile aynı hex'ler).
export const colors = {
  navy: '#12235c',
  navyDeep: '#0d1a44',
  navy2: '#16265c',
  blue: '#1f5fc0',
  blueBright: '#1c7ed6',
  cyan: '#22b8e8',
  cyanSoft: '#e6f4fc',
  green: '#2f9e44',
  greenSoft: '#e7f6ec',
  amber: '#f59f00',
  orange: '#f76707',
  red: '#e03131',
  redSoft: '#fdecea',
  ink: '#16213d',
  slate: '#5a6a86',
  muted: '#8f9cb8',
  line: '#e1e8f4',
  lineStrong: '#cfd9ec',
  bg: '#f4f8fe',
  surface: '#ffffff',
  surfaceAlt: '#eef4fd',
  white: '#ffffff'
} as const;

export const gradients = {
  brand: ['#1f4bb0', '#16265c'] as const,
  hero: ['#1c65c9', '#12235c'] as const,
  cyan: ['#22b8e8', '#1c7ed6'] as const,
  deep: ['#16265c', '#0d1a44'] as const
};

export const radius = { sm: 10, md: 16, lg: 22, xl: 28, pill: 999 } as const;

export const spacing = (n: number) => n * 4;

export const shadow = (elevation = 8): ViewStyle =>
  Platform.select<ViewStyle>({
    ios: {
      shadowColor: '#0b1b45',
      shadowOpacity: 0.12,
      shadowRadius: elevation,
      shadowOffset: { width: 0, height: elevation / 2 }
    },
    android: { elevation: elevation / 2 },
    default: {}
  }) as ViewStyle;

export const type = {
  h1: { fontSize: 26, fontWeight: '800', color: colors.ink, letterSpacing: -0.3 } as TextStyle,
  h2: { fontSize: 20, fontWeight: '800', color: colors.ink, letterSpacing: -0.2 } as TextStyle,
  h3: { fontSize: 16, fontWeight: '700', color: colors.ink } as TextStyle,
  body: { fontSize: 15, fontWeight: '500', color: colors.slate, lineHeight: 21 } as TextStyle,
  small: { fontSize: 13, fontWeight: '500', color: colors.muted } as TextStyle,
  label: { fontSize: 13, fontWeight: '700', color: colors.navy, letterSpacing: 0.2 } as TextStyle
};
