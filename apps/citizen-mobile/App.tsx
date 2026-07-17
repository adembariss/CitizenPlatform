import { Ionicons } from '@expo/vector-icons';
import { NavigationContainer, DefaultTheme } from '@react-navigation/native';
import { createBottomTabNavigator } from '@react-navigation/bottom-tabs';
import { StatusBar } from 'expo-status-bar';
import { Platform } from 'react-native';
import { SafeAreaProvider } from 'react-native-safe-area-context';
import { AuthProvider } from './src/context/AuthContext';
import { AccountScreen } from './src/screens/AccountScreen';
import { HomeScreen } from './src/screens/HomeScreen';
import { PharmaciesScreen } from './src/screens/PharmaciesScreen';
import { ReportScreen } from './src/screens/ReportScreen';
import { TrackScreen } from './src/screens/TrackScreen';
import { colors } from './src/theme';

const Tab = createBottomTabNavigator();

const ICONS: Record<string, { on: keyof typeof Ionicons.glyphMap; off: keyof typeof Ionicons.glyphMap }> = {
  Ana: { on: 'home', off: 'home-outline' },
  Bildir: { on: 'megaphone', off: 'megaphone-outline' },
  Eczaneler: { on: 'medkit', off: 'medkit-outline' },
  Takip: { on: 'search', off: 'search-outline' },
  Hesap: { on: 'person', off: 'person-outline' }
};

const navTheme = {
  ...DefaultTheme,
  colors: { ...DefaultTheme.colors, background: colors.bg, primary: colors.blue }
};

export default function App() {
  return (
    <SafeAreaProvider>
      <AuthProvider>
        <StatusBar style="light" />
        <NavigationContainer theme={navTheme}>
          <Tab.Navigator
            initialRouteName="Ana"
            screenOptions={({ route }) => ({
              headerShown: false,
              tabBarActiveTintColor: colors.blue,
              tabBarInactiveTintColor: colors.muted,
              tabBarStyle: {
                height: Platform.OS === 'ios' ? 86 : 66,
                paddingTop: 8,
                paddingBottom: Platform.OS === 'ios' ? 28 : 10,
                backgroundColor: colors.surface,
                borderTopColor: colors.line
              },
              tabBarLabelStyle: { fontSize: 11, fontWeight: '700' },
              tabBarIcon: ({ focused, color, size }) => {
                const icon = ICONS[route.name];
                return <Ionicons name={focused ? icon.on : icon.off} size={size - 2} color={color} />;
              }
            })}
          >
            <Tab.Screen name="Ana" component={HomeScreen} options={{ title: 'Ana Sayfa' }} />
            <Tab.Screen name="Bildir" component={ReportScreen} options={{ title: 'Bildir' }} />
            <Tab.Screen name="Eczaneler" component={PharmaciesScreen} options={{ title: 'Eczane' }} />
            <Tab.Screen name="Takip" component={TrackScreen} options={{ title: 'Takip' }} />
            <Tab.Screen name="Hesap" component={AccountScreen} options={{ title: 'Hesabım' }} />
          </Tab.Navigator>
        </NavigationContainer>
      </AuthProvider>
    </SafeAreaProvider>
  );
}
