import { type RouteConfig, index, layout, route } from "@react-router/dev/routes";

export const AppPaths = {
  home: "/",
  game: "/game",
  history: "/history",
  about: "/about"
}

export default [
  layout("./common-components/app-default-layout.tsx", [
    index("./home.tsx"),

    route(AppPaths.game, "./game/index.tsx"),
    
    route(AppPaths.history, "./history/index.tsx"),

    route(AppPaths.about, "./about/index.tsx")
  ])

] satisfies RouteConfig;
