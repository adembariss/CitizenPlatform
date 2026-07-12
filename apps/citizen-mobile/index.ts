// Local entry point instead of "expo/AppEntry.js": in this npm-workspaces monorepo the
// hoisted node_modules/expo/AppEntry.js resolves "../../App" against the repo root, so the
// root component must be registered from inside the app package itself.
import { registerRootComponent } from 'expo';

import App from './App';

registerRootComponent(App);
