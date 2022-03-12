<template>
    <v-snackbar v-model="snackbar" :timeout="timeout">
        {{ currentMessage }}
        <v-btn text color="primary" @click.native="closeSnackbar()"
            >Close</v-btn
        >
    </v-snackbar>
</template>

<script>
import { mapState } from "pinia";
import { snackbarState } from "@/stores/snackbarState";

export default {
    data: () => ({
        timeout: 4000,
    }),
    computed: {
        ...mapState(snackbarState, [
            "currentMessage",
            "isVisible",
            "closeSnackbar",
            "openSnackbar",
        ]),
        snackbar: {
            get() {
                return this.isVisible;
            },
            set() {
                this.closeSnackbar();
            },
        },
    },
};
</script>
