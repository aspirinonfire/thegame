import { reactRouter } from "@react-router/dev/vite";
import tailwindcss from "@tailwindcss/vite";
import { defineConfig } from "vite";
import tsconfigPaths from "vite-tsconfig-paths";
import flowbiteReact from "flowbite-react/plugin/vite";
import packageJson from "./package.json";

export default defineConfig({
  plugins: [tailwindcss(), reactRouter(), tsconfigPaths(), flowbiteReact()],
  // this step is required for build to work properly.
  // see https://www.reddit.com/r/nextjs/comments/1cu6vnw/comment/lr3j4mv/?utm_source=share&utm_medium=web3x&utm_name=web3xcss&utm_term=1&utm_content=share_button
  build: {
    rollupOptions: {
      external: Object.keys(packageJson.peerDependencies),
    },
  }
});