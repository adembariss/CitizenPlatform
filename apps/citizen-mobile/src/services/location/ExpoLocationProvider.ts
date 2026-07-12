import * as Location from 'expo-location';
import { Coordinates, LocationProvider } from './LocationProvider';

export class ExpoLocationProvider implements LocationProvider {
  async getCurrentPosition(): Promise<Coordinates> {
    const { status } = await Location.requestForegroundPermissionsAsync();
    if (status !== 'granted') {
      throw new Error('Konum izni verilmedi.');
    }

    const position = await Location.getCurrentPositionAsync({ accuracy: Location.Accuracy.High });
    return { latitude: position.coords.latitude, longitude: position.coords.longitude };
  }
}
