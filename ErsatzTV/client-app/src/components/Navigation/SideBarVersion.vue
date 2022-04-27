<template>
    <span v-if="!isNavigationMini" class="text-center">
        <v-divider inset></v-divider>
        <h4 class="pt-2">ErsatzTV {{ $t('sidebar.version') }}</h4>

        <p>{{ currentServerVersion }}</p>
    </span>
</template>

<script>
import { mapState } from 'pinia';
import { applicationState } from '@/stores/applicationState';

export default {
    name: 'SideBarVersion',
    setup() {
        applicationState().getVersion();
    },
    computed: {
        ...mapState(applicationState, [
            'currentServerVersion',
            'isNavigationMini'
        ])
    }
};
</script>

<style scoped></style>
