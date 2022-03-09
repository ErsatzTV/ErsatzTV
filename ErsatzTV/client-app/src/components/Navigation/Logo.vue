<template>
    <span>
        <v-img v-if="!mini" src="@/assets/images/ersatztv.png" class="ma-3"></v-img>
        <v-img v-if="mini" src="@/assets/images/ersatztv-500.png"></v-img>
    </span>

</template>

<script>
export default {
    name: "Logo",
    props:{
        
    }
}
</script>