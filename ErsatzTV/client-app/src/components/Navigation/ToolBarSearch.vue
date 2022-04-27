<template>
    <v-text-field
        outlined
        class="ml-5"
        :label="$t('tool-bar.search')"
        prepend-inner-icon="mdi-magnify"
        hide-details
        dense
    ></v-text-field>
</template>

<script>
export default {
    name: 'ToolBarSearch'
};
</script>

<style scoped></style>
