import Svg, { Defs, LinearGradient, Path, Rect, Stop } from 'react-native-svg';

// NetCAD "N" esinli marka işareti: lacivert yuvarlak kare + cyan N + beyaz vurgu.
export function BrandMark({ size = 40 }: { size?: number }) {
  return (
    <Svg width={size} height={size} viewBox="0 0 40 40">
      <Defs>
        <LinearGradient id="brandGrad" x1="0" y1="0" x2="1" y2="1">
          <Stop offset="0" stopColor="#1f4bb0" />
          <Stop offset="1" stopColor="#16265c" />
        </LinearGradient>
      </Defs>
      <Rect width="40" height="40" rx="11" fill="url(#brandGrad)" />
      <Path d="M11 29V12h3.4l8.2 10.4V12H26v17h-3.4l-8.2-10.4V29H11Z" fill="#22b8e8" />
      <Rect x="25.5" y="11" width="6.2" height="6.2" rx="1.2" fill="#ffffff" />
    </Svg>
  );
}
