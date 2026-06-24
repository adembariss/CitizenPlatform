export type Coordinates = {
  latitude: number;
  longitude: number;
};

export interface LocationProvider {
  getCurrentPosition(): Promise<Coordinates>;
}
