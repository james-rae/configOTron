<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class ConfigForm
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.cmdEnhance = New System.Windows.Forms.Button()
        Me.cmdEnhanceMini = New System.Windows.Forms.Button()
        Me.SuspendLayout()
        '
        'cmdEnhance
        '
        Me.cmdEnhance.Location = New System.Drawing.Point(54, 35)
        Me.cmdEnhance.Name = "cmdEnhance"
        Me.cmdEnhance.Size = New System.Drawing.Size(304, 87)
        Me.cmdEnhance.TabIndex = 0
        Me.cmdEnhance.Text = "Enhance - Full Configs"
        Me.cmdEnhance.UseVisualStyleBackColor = True
        '
        'cmdEnhanceMini
        '
        Me.cmdEnhanceMini.Location = New System.Drawing.Point(54, 142)
        Me.cmdEnhanceMini.Name = "cmdEnhanceMini"
        Me.cmdEnhanceMini.Size = New System.Drawing.Size(304, 87)
        Me.cmdEnhanceMini.TabIndex = 1
        Me.cmdEnhanceMini.Text = "Enhance - Mini Configs"
        Me.cmdEnhanceMini.UseVisualStyleBackColor = True
        '
        'ConfigForm
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(435, 262)
        Me.Controls.Add(Me.cmdEnhanceMini)
        Me.Controls.Add(Me.cmdEnhance)
        Me.Name = "ConfigForm"
        Me.Text = "Config O Tron"
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents cmdEnhance As Button
    Friend WithEvents cmdEnhanceMini As Button
End Class
