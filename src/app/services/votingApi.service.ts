import { HttpClient, HttpHeaders } from '@angular/common/http';
import { inject, Injectable } from "@angular/core";
import { environment } from "../../environments/environment";

@Injectable({
  providedIn: 'root'
})
export class votingAPI {
  http: HttpClient = inject(HttpClient);
  url: string = environment.apiUrl

  verifyNationalNumber(electionID, nationalID) {
    let data: Object = {
      electionId: electionID,
      nationalId: nationalID
    }

    return this.http.post(`${this.url}/api/voting/verify-identity`, data)
  }


  verifyPhoto(electionId, nationalId: string, selfieBase64: string) {

    let data: Object = {
      electionId: electionId,
      nationalId: nationalId,
      selfieBase64: selfieBase64
    }

    return this.http.post(`${this.url}/api/voting/verify-face`, data)
  }


  castVote(electionId, candidateId, voterToken: string) {
    let data: Object = {
      electionId: electionId,
      candidateId: candidateId
    }

    let token: string = voterToken;
    const headers = new HttpHeaders().set('X-Voting-Token', token);

    return this.http.post(`${this.url}/api/voting/cast-vote`, data, { headers })
  }

}
