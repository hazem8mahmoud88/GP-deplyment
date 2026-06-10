export class Admin {
  constructor(
    public email: string,
    public username: string,
    public role: string,
    public adminId: number,
    public organizerId,
    public expiresAt: Date,
    private _token: string,
  ){}

  get token() {
    if (!this.expiresAt || new Date() > this.expiresAt) {
      return null
    }

    return this._token
  }

}
