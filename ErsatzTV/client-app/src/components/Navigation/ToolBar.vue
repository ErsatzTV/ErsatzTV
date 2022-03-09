<template>
    <nav>
        <v-app-bar
            flat
            app
            dense
            absolute
        >
            <v-app-bar-nav-icon @click.stop="toggleMiniNavigation()" />

            <v-spacer />
   
        </v-app-bar>
        <v-navigation-drawer
            app
            :mini-variant="isNavigationMini"
            :permanent="!this.$vuetify.breakpoint.xsOnly"
        >
            <SideBarLogo />
            <SideBarMenu />
            <template v-slot:append>
                <SideBarVersion />
            </template>
        </v-navigation-drawer>
    </nav>
</template>

<script>
import SideBarLogo from "./SideBarLogo.vue";
import SideBarMenu from "./SideBarMenu.vue";
import SideBarVersion from "./SideBarVersion";
import { mapState } from 'pinia';
import { applicationState } from '@/stores/applicationState';

export default {
    name: "NavToolbar",
    components: { SideBarMenu, SideBarLogo, SideBarVersion },
    computed: {
        ...mapState(applicationState, ['isNavigationMini', 'toggleMiniNavigation'])
    }
};
</script>