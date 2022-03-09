<template>
    <nav>
        <v-app-bar
            flat
            app
            dense
            absolute
        >
            <v-app-bar-nav-icon @click.stop="toggle()" />

            <v-spacer />
   
        </v-app-bar>
        <v-navigation-drawer
            app
            :mini-variant="mini"
            permanent
        >
            <SideBarLogo />
            <Navigation @update_nav_drawer="update_drawer" />
        </v-navigation-drawer>
    </nav>
</template>

<script>
import SideBarLogo from "./SideBarLogo.vue";
import Navigation from "./SideBarMenu.vue";
import { mapState } from 'pinia'
import { useMenuToggleStore } from '@/stores/menuToggle'

export default {
    name: "NavToolbar",
    components: { Navigation, SideBarLogo },
    data: () => ({
        menu_reload_key: 0,
    }),
    computed: {
        ...mapState(useMenuToggleStore, ['mini', 'toggle'])
    },
    methods: {
        update_drawer(toggle) {
            this.mini = toggle;
        },
    },
};
</script>