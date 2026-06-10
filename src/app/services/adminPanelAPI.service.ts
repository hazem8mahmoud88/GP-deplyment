import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from "@angular/core";

@Injectable({
  providedIn: 'root'
})
export class AdminPanelAPI {
  baseUrl: string = 'https://localhost:7087/api'
  httpClient: HttpClient = inject(HttpClient)

  addOrganizer(id: number, organizerID: number) {
    let organizerInfo: Object = {
      organizerId: organizerID,
      canDecrypt: true
    }

    console.log("error in Add");
    console.log("Base URL", this.baseUrl);
    console.log("Election ID", id);
    console.log("Org Info", organizerInfo);

    return this.httpClient.post(`${this.baseUrl}/elections/${id}/organizers`, organizerInfo)
  }

  getOrganizers(id: number) {
    return this.httpClient.get(`${this.baseUrl}/elections/${id}/organizers`)
  }

  getAllOrganizers() {
    return this.httpClient.get(`${this.baseUrl}/admin/organizers`);
  }

  deleteOrganizer(id: number, organizerID: number) {
    console.log("error in delete");
    return this.httpClient.delete(`${this.baseUrl}/elections/${id}/organizers/${organizerID}`)
  }
}
