import { type RouteConfig, index, layout, route } from "@react-router/dev/routes";

/**
 * Note, `id` option is required becayse we are reusing "layout" component
 */
export default [
  layout("./common-components/app-default-layout.tsx", [
    index("./home.tsx"),

    route("/about", "./about/index.tsx")
  ])

] satisfies RouteConfig;
