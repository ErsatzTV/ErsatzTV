export interface FFmpegFullProfile {
    Id: number;
    name: string;
    threadCount: number;
    hardwareAcceleration: number;
    vaapiDriver: number;
    vaapiDevice: string;
    resolutionId: number;
    videoFormat: number;
    videoBitrate: number;
    videoBufferSize: number;
    audioFormat: number;
    audioBitrate: number;
    audioBufferSize: number;
    normalizeLoudness: boolean;
    audioChannels: number;
    audioSampleRate: number;
    normalizeFramerate: boolean;
    deinterlaceVideo: boolean;
}
