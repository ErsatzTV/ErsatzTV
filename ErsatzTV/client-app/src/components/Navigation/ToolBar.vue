<template>
    <nav>
        <v-app-bar
            flat
            app
            dense
        >
            <v-app-bar-nav-icon @click.stop="mini = !mini" />

            <v-spacer />
            
        </v-app-bar>
        <v-navigation-drawer
            app
            :mini-variant="mini"
            permanent
        >

            <Navigation @update_nav_drawer="update_drawer" />
        </v-navigation-drawer>
    </nav>
</template>

<script>
import Navigation from "./SideBar.vue";

export default {
    name: "NavToolbar",
    components: { Navigation },
    data: () => ({
        mini: true,
        menu_reload_key: 0,
    }),
    methods: {
        update_drawer(toggle) {
            this.mini = toggle;
        },
    },
};
</script>