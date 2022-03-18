import { AbstractApiService } from './AbstractApiService';
import { FFmpegProfile } from '@/models/FFmpegProfile';

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
}

export const ffmpegProfileApiService: FFmpegProfileApiService =
    new FFmpegProfileApiService();
