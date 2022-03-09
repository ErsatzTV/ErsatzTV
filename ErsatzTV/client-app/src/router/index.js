import Vue from "vue";
import VueRouter from "vue-router";
import HomePage from "../views/HomePage.vue";

Vue.use(VueRouter);

const routes = [
    {
        path: "/",
        name: "Home",
        component: HomePage,
        meta: {
            icon: "mdi-home",
            disabled: false
        }
    },
    {
        path: "/Settings",
        name: "Settings",
        component: HomePage,
        meta: {
            icon: "mdi-home",
            disabled: true
        }
    },
];

const router = new VueRouter({
    mode: "history",
    base: process.env.BASE_URL,
    routes,
});

export default router;