// Expo monorepo config (https://docs.expo.dev/guides/monorepos/): without this, hoisted
// packages such as react-native-web resolve "react" from the workspace root (18.3.1 for the
// Vite apps) while app code gets the local 18.2.0, and two React instances crash at runtime.
const { getDefaultConfig } = require('expo/metro-config');
const path = require('path');

const projectRoot = __dirname;
const workspaceRoot = path.resolve(projectRoot, '../..');

const config = getDefaultConfig(projectRoot);

config.watchFolders = [workspaceRoot];
config.resolver.nodeModulesPaths = [
  path.resolve(projectRoot, 'node_modules'),
  path.resolve(workspaceRoot, 'node_modules')
];
config.resolver.disableHierarchicalLookup = true;

module.exports = config;
