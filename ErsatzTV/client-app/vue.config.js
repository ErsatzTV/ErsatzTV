const { defineConfig } = require("@vue/cli-service");

module.exports = defineConfig({
    transpileDependencies: ["vuetify"],
    runtimeCompiler: true,

    pwa: {
        name: "ErsatzTV",
    },
    publicPath: "/v2/",
    outputDir: "../wwwroot/v2",
    filenameHashing: false,
});
