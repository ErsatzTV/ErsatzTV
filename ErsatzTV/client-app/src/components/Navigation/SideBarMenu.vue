<template>
    <v-list
        nav
        dense
    >
    <span
        v-for="(nav, i) in navigation"
        :key="i"
    >
        <SideBarMenuItem
            v-if="!nav.children"
            :name="nav.name" 
            :path="nav.path" 
            :icon="nav.meta.icon" 
            :disabled="nav.meta.disabled"
        /> 
        <SideBarMenuItemExpandable
            v-else
            :name="nav.name"
            :icon="nav.meta.icon"
            :disabled="nav.meta.disabled"
            :children="nav.children"
        /> 
    </span>
    </v-list>
</template>

<script>
import SideBarMenuItem from "./SideBarMenuItem"
import SideBarMenuItemExpandable from "./SideBarMenuItemExpandable"

export default {
    name: "NavSidebar",
    components: { SideBarMenuItem, SideBarMenuItemExpandable },
    data: () => ({
        navigation: null,
    }),
    beforeMount: function() {
        //Pull in navigation from routes and load into DOM
        this.navigation = this.$router.options.routes;
    }
};
</script>