<template>
    <v-app>
        <Toolbar />
        <!-- Sizes your content based upon application components -->
        <v-main>
            <!-- Provides the application the proper gutter -->
            <v-container fluid>
                <!-- If using vue-router -->
                <router-view></router-view>
            </v-container>
        </v-main>

        <v-footer app>
            <!-- -->
        </v-footer>
    </v-app>
</template>

<script>
import Vue from "vue";
import Toolbar from "@/components/Navigation/ToolBar.vue";
export default Vue.extend({
    name: "App",
    components: { Toolbar },
    data: () => ({})
});
</script>
<style>
main {
    background-color: black;
    background-attachment: fixed;
    background-repeat: no-repeat;
    -webkit-background-size: cover;
    -moz-background-size: cover;
    -o-background-size: cover;
    background-size: cover;
    background-position: center;
}
</style>