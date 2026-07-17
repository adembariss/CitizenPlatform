import * as Location from 'expo-location';

export type Coordinates = { latitude: number; longitude: number };

// Konum iznini ister ve mevcut konumu döndürür. Reddedilirse anlamlı bir hata fırlatır.
export async function getCurrentPosition(): Promise<Coordinates> {
  const { status } = await Location.requestForegroundPermissionsAsync();
  if (status !== 'granted') {
    throw new Error('Konum izni verilmedi. Ayarlardan izin verebilir veya haritadan konum seçebilirsiniz.');
  }
  const position = await Location.getCurrentPositionAsync({ accuracy: Location.Accuracy.High });
  return { latitude: position.coords.latitude, longitude: position.coords.longitude };
}
