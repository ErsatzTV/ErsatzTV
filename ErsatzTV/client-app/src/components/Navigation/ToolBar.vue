<template>
    <nav>
        <v-app-bar flat app dense absolute>
            <v-app-bar-nav-icon @click.stop="toggleMiniNavigation()" />

            <ToolBarSearch />
            <v-spacer />

            <ToolBarLinks />
        </v-app-bar>
        <v-navigation-drawer app :mini-variant="isNavigationMini" permanent>
            <template v-slot:prepend>
                <SideBarLogo />
            </template>

            <SideBarMenu />

            <template v-slot:append>
                <SideBarLanguageSelect />
                <SideBarVersion />
            </template>
        </v-navigation-drawer>
    </nav>
</template>

<script>
import SideBarLogo from './SideBarLogo.vue';
import SideBarMenu from './SideBarMenu.vue';
import SideBarVersion from './SideBarVersion';
import ToolBarLinks from './ToolBarLinks';
import ToolBarSearch from './ToolBarSearch';
import { mapState } from 'pinia';
import { applicationState } from '@/stores/applicationState';
import SideBarLanguageSelect from './SideBarLanguageSelect.vue';

export default {
    name: 'NavToolbar',
    components: {
        SideBarMenu,
        SideBarLogo,
        SideBarVersion,
        ToolBarLinks,
        ToolBarSearch,
        SideBarLanguageSelect
    },
    computed: {
        ...mapState(applicationState, [
            'isNavigationMini',
            'toggleMiniNavigation'
        ])
    }
};
</script>
