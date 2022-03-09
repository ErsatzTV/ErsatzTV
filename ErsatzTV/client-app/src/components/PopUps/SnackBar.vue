<template>
    <v-snackbar 
        v-model="snackbar"
        :timeout="timeout"
    >
        {{snackbarStatus.currentMessage}}
        <v-btn flat color="primary" @click.native="closeSnackbar()">Close</v-btn>
    </v-snackbar>
</template>

<script>
import { mapState } from 'pinia';
import { snackbarState } from '@/stores/snackbarState';

export default {
    data: () => ({
        timeout: 4000,
    }),
    computed: {
        ...mapState(snackbarState, ['snackbarStatus', 'closeSnackbar', 'openSnackbar']),
        snackbar: {
            get() {
                return this.snackbarStatus.isVisible
            },
            set() {
                this.closeSnackbar();
            }
        }
    },
}
</script>