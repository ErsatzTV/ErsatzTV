<template>
    <span>
        <v-img v-if="!isNavigationMini" src="@/assets/images/ersatztv.png" class="ma-3"></v-img>
        <v-img v-if="isNavigationMini" src="@/assets/images/ersatztv-500.png"></v-img>
    </span>

</template>

<script>
import { mapState } from 'pinia'
import { applicationState } from '@/stores/applicationState'

export default {
    name: "SideBarLogo",
    computed: {
        ...mapState(applicationState, ['isNavigationMini'])
    },
}
</script>