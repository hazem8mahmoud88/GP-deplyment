import { HttpClient } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";


@Injectable({
  providedIn: 'root'
})
export class OrganizerPanel {
  baseUrl: string = 'https://localhost:7087/api'
  httpClient: HttpClient = inject(HttpClient);

  getOrganizerElections(id: number) {
    return this.httpClient.get(`${this.baseUrl}/organizer/my-elections`)
  }

  uploadVoters(electionID: string, votersFile: File) {
    const formData = new FormData();
    formData.append('csvFile', votersFile)
    return this.httpClient.post(`${this.baseUrl}/organizer/elections/${electionID}/upload-voters`, formData);
  }

  uploadPhotos(electionID: string, photosFile: File) {
    const formData = new FormData();
    formData.append('zipFile', photosFile)
    return this.httpClient.post(`${this.baseUrl}/organizer/elections/${electionID}/upload-photos`, formData);
  }

  addCandidates(electionID: string,fullName: string, symbol: string, partyName: string, candidateNum: string) {
    const data = {
      fullName: fullName,
      symbol: symbol,
      partyName: partyName,
      photoPath: null,
      orderNumber: candidateNum
    }
    return this.httpClient.post(`${this.baseUrl}/elections/${electionID}/Candidates`, data);
  }

  getCandidates(electionID: string) {
    return this.httpClient.get(`${this.baseUrl}/elections/${electionID}/Candidates`);
  }

  deleteCandidate(electionID: string,candidateID: number) {
    return this.httpClient.delete(`${this.baseUrl}/elections/${electionID}/Candidates/${candidateID}`)
  }

  uploadCandidatePicture(formData: FormData, electionID, candidateID: number) {
    return this.httpClient.post(`${this.baseUrl}/elections/${electionID}/Candidates/${candidateID}/photo`, formData)
  }

}
