import { defineStore } from 'pinia';

export const snackbarState = defineStore('snackbarState', {
    state: () => {
        return {
            visible: false,
            message: ''
        };
    },
    getters: {
        isVisible(state) {
            return state.visible;
        },
        currentMessage(state) {
            return state.message;
        }
    },
    actions: {
        showSnackbar(message) {
            this.message = message;
            this.visible = true;
        },
        closeSnackbar() {
            this.visible = false;
        }
    }
});
