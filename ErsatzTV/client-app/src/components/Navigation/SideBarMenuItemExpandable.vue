<template>
    <v-list-group
        color="primary"
        v-model="menuItemOpened"
        :disabled="disabled"
        :prepend-icon="icon"
        no-action
    >
        <template v-slot:activator>
            <v-list-item-content>
                <v-list-item-title v-text="name"></v-list-item-title>
            </v-list-item-content>
        </template>

        <v-list-item
            v-for="child in children"
            :key="child.name"
            :to="child.path"
            :disabled="child.meta.disabled"
        >
            <v-list-item-icon>
                <v-icon
                    v-text="child.meta.icon"
                    :disabled="child.meta.disabled"
                />
            </v-list-item-icon>
            <v-list-item-content>
                <v-list-item-title v-text="child.name"></v-list-item-title>
            </v-list-item-content>
        </v-list-item>
    </v-list-group>
</template>

<script>
export default {
    name: "SideBarMenuItemExpandable",
    props: {
        name: {
            type: String,
            required: true,
        },
        icon: {
            type: String,
            required: true,
        },
        disabled: {
            type: Boolean,
            required: true,
        },
        children: {
            type: Array,
            required: true,
        },
    },
    data: () => ({
        opened: false,
    }),
    computed: {
        menuItemOpened: {
            get: function () {
                return this.opened;
            },
            set: function () {
                return !this.opened;
            },
        },
    },
};
</script>
