import { AbstractApiService } from './AbstractApiService';
import { FFmpegProfile } from '@/models/FFmpegProfile';
import { FFmpegFullProfile } from '../models/FFmpegFullProfile';

class FFmpegProfileApiService extends AbstractApiService {
    public constructor() {
        super();
    }

    public getAll(): Promise<FFmpegProfile[]> {
        return this.http
            .get('/api/ffmpeg/profiles')
            .then(this.handleResponse.bind(this))
            .catch(this.handleError.bind(this));
    }

    public getOne(id: string): Promise<FFmpegFullProfile[]> {
        return this.http
            .get('/api/ffmpeg/profiles/' + id)
            .then(this.handleResponse.bind(this))
            .catch(this.handleResponse.bind(this));
    }

    public newFFmpegProfile(
        Name: string,
        ThreadCount: number,
        HardwareAcceleration: number,
        VaapiDriver: number,
        VaapiDevice: string,
        ResolutionId: number,
        VideoFormat: number,
        VideoBitrate: number,
        VideoBufferSize: number,
        AudioFormat: number,
        AudioBitrate: number,
        AudioBufferSize: number,
        NormalizeLoudness: boolean,
        AudioChannels: number,
        AudioSampleRate: number,
        NormalizeFramerate: boolean,
        DeinterlaceVideo: boolean
    ) {
        const data = {
            Name: Name,
            ThreadCount: ThreadCount,
            HardwareAcceleration: HardwareAcceleration,
            VaapiDriver: VaapiDriver,
            VaapiDevice: VaapiDevice,
            ResolutionId: ResolutionId,
            VideoFormat: VideoFormat,
            VideoBitrate: VideoBitrate,
            VideoBufferSize: VideoBufferSize,
            AudioFormat: AudioFormat,
            AudioBitrate: AudioBitrate,
            AudioBufferSize: AudioBufferSize,
            NormalizeLoudness: NormalizeLoudness,
            AudioChannels: AudioChannels,
            AudioSampleRate: AudioSampleRate,
            NormalizeFramerate: NormalizeFramerate,
            DeinterlaceVideo: DeinterlaceVideo
        };
        this.http.post('/api/ffmpeg/profiles/new', data);
    }

    public updateFFmpegProfile(
        Id: number,
        Name: string,
        ThreadCount: number,
        HardwareAcceleration: number,
        VaapiDriver: number,
        VaapiDevice: string,
        ResolutionId: number,
        VideoFormat: number,
        VideoBitrate: number,
        VideoBufferSize: number,
        AudioFormat: number,
        AudioBitrate: number,
        AudioBufferSize: number,
        NormalizeLoudness: boolean,
        AudioChannels: number,
        AudioSampleRate: number,
        NormalizeFramerate: boolean,
        DeinterlaceVideo: boolean
    ) {
        const data = {
            Id: Id,
            Name: Name,
            ThreadCount: ThreadCount,
            HardwareAcceleration: HardwareAcceleration,
            VaapiDriver: VaapiDriver,
            VaapiDevice: VaapiDevice,
            ResolutionId: ResolutionId,
            VideoFormat: VideoFormat,
            VideoBitrate: VideoBitrate,
            VideoBufferSize: VideoBufferSize,
            AudioFormat: AudioFormat,
            AudioBitrate: AudioBitrate,
            AudioBufferSize: AudioBufferSize,
            NormalizeLoudness: NormalizeLoudness,
            AudioChannels: AudioChannels,
            AudioSampleRate: AudioSampleRate,
            NormalizeFramerate: NormalizeFramerate,
            DeinterlaceVideo: DeinterlaceVideo
        };
        this.http.put('/api/ffmpeg/profiles/update', data);
    }

    public deleteRecord(id: string) {
        this.http.delete('/api/ffmpeg/delete/' + id);
    }
}

export const ffmpegProfileApiService: FFmpegProfileApiService =
    new FFmpegProfileApiService();
