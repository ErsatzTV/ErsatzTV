<template>
    <v-list-item-group color="primary">
        <v-list-item :to="path" :disabled="disabled">
            <v-list-item-icon>
                <v-icon v-text="icon" :disabled="disabled" />
            </v-list-item-icon>

            <v-list-item-content>
                <v-list-item-title v-text="name" />
            </v-list-item-content>
        </v-list-item>
    </v-list-item-group>
</template>

<script>
export default {
    name: 'SideBarMenuItem',
    props: {
        name: {
            type: String,
            required: true
        },
        path: {
            type: String,
            required: true
        },
        icon: {
            type: String,
            required: true
        },
        disabled: {
            type: Boolean,
            required: true
        }
    }
};
</script>
