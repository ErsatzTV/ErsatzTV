<template>
    <span>
        <v-tooltip bottom>
            <template v-slot:activator="{ on, attrs }">
                <v-btn
                    class="ma-2"
                    outlined
                    color="primary"
                    @click="copyTextToClipboard(navBarURLs.m3uURL)"
                    v-bind="attrs"
                    v-on="on"
                    small
                >
                    <v-icon>mdi-playlist-play</v-icon> M3U
                </v-btn>
            </template>
            <span>{{ $t('tool-bar.click-to-copy') }}</span>
        </v-tooltip>

        <v-tooltip bottom>
            <template v-slot:activator="{ on, attrs }">
                <v-btn
                    class="ma-2"
                    outlined
                    color="primary"
                    @click="copyTextToClipboard(navBarURLs.xmlURL)"
                    v-bind="attrs"
                    v-on="on"
                    small
                >
                    <v-icon>mdi-xml</v-icon> XML
                </v-btn>
            </template>
            <span>{{ $t('tool-bar.click-to-copy') }}</span>
        </v-tooltip>

        <v-btn
            icon
            color="secondary"
            :href="navBarURLs.documentationURL"
            target="_blank"
        >
            <v-icon>mdi-file-document</v-icon>
        </v-btn>

        <v-btn
            icon
            color="secondary"
            :href="navBarURLs.discordURL"
            target="_blank"
        >
            <v-icon>mdi-discord</v-icon>
        </v-btn>

        <v-btn
            icon
            color="secondary"
            :href="navBarURLs.githubURL"
            target="_blank"
        >
            <v-icon>mdi-github</v-icon>
        </v-btn>
    </span>
</template>

<script>
import { mapState } from 'pinia';
import { applicationState } from '@/stores/applicationState';
import { snackbarState } from '@/stores/snackbarState';

export default {
    name: 'ToolBarLinks.vue',
    data: () => ({
        toast: false
    }),
    computed: {
        ...mapState(applicationState, ['navBarURLs']),
        ...mapState(snackbarState, ['showSnackbar'])
    },
    methods: {
        copyTextToClipboard(text) {
            try {
                navigator.clipboard.writeText(text);
                this.showSnackbar(this.$t('tool-bar.copy-success'));
            } catch (error) {
                console.error(error);
                this.showSnackbar(
                    this.$t('tool-bar.copy-failure', { message: error })
                );
            }
        }
    }
};
</script>

<style scoped></style>
