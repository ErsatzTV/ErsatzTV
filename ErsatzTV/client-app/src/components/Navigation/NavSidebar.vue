<template>
    <v-list
        nav
        dense
    >
    <span
        v-for="(nav, i) in navigation"
        :key="i"
    >
      <v-list-item-group color="primary">
        <v-list-item
            :to="nav.path"
            @click="toggle_tool_bar(true)"
        >
          <v-list-item-icon>
            <v-icon v-text="nav.icon" />
          </v-list-item-icon>

          <v-list-item-content>
            <v-list-item-title v-text="nav.name" />
          </v-list-item-content>
        </v-list-item>
      </v-list-item-group>
    </span>
    </v-list>
</template>

<script>
export default {
    name: "NavSidebar",
    data: () => ({
        navigation: null
    }),
    methods: {
        toggle_tool_bar(toggle) {
            if (toggle) {
                this.$emit("update_nav_drawer", true);
            } else {
                this.$emit("update_nav_drawer", false);
            }
        }
    },
    beforeMount: function() {
        //Pull in navigation from routes and load into DOM
        this.navigation = this.$router.options.routes;
    }
};
</script>